using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SstControl.Aplicacion.Integraciones;
using SstControl.Dominio.Entidades;

namespace SstControl.Api.Controladores;

/// <summary>Datos para sincronizar manualmente una reunión externa hacia el sistema central.</summary>
public record PeticionSincronizarReunion(string IdReunionExterna, int IdEmpresa, int IdSede, string Tipo);

/// <summary>Sincronización on-demand de reuniones desde Teams, Google Meet o Zoom.
/// Requiere el permiso "reuniones.sincronizar".</summary>
[ApiController]
[Authorize(Policy = "reuniones.sincronizar")]
[Route("api/sincronizacion-reuniones")]
public class SincronizacionReunionesControlador : ControllerBase
{
    private readonly IServicioSincronizacionReuniones _servicioSincronizacion;
    public SincronizacionReunionesControlador(IServicioSincronizacionReuniones servicioSincronizacion) => _servicioSincronizacion = servicioSincronizacion;

    /// <summary>POST /api/sincronizacion-reuniones/{proveedor} — trae la reunión, sus
    /// asistentes y su contenido desde la plataforma indicada, y los persiste como Acta.</summary>
    [HttpPost("{proveedor}")]
    public async Task<IActionResult> Sincronizar(string proveedor, PeticionSincronizarReunion peticion)
    {
        if (!Enum.TryParse<ProveedorReunion>(proveedor, ignoreCase: true, out var proveedorResuelto))
            return BadRequest(new { mensaje = "Proveedor no soportado. Usa teams, googlemeet, zoom o webex." });

        var idUsuario = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var tipo = Enum.Parse<TipoActa>(peticion.Tipo, ignoreCase: true);

        var idActa = await _servicioSincronizacion.SincronizarReunionAsync(
            proveedorResuelto, peticion.IdReunionExterna, peticion.IdEmpresa, peticion.IdSede, tipo, idUsuario);
        return Ok(new { idActa });
    }
}

/// <summary>
/// Receptores de webhooks para sincronización casi en tiempo real cuando la reunión
/// termina. Cada plataforma exige registrar esta URL pública en su panel de developer
/// y validar la firma de cada evento antes de confiar en su contenido.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/webhooks")]
public class WebhooksReunionesControlador : ControllerBase
{
    private readonly IServicioSincronizacionReuniones _servicioSincronizacion;
    private readonly IServicioMapeoReunion _servicioMapeo;
    private readonly IConfiguration _configuracion;
    private readonly ILogger<WebhooksReunionesControlador> _logger;
    public WebhooksReunionesControlador(IServicioSincronizacionReuniones servicioSincronizacion,
        IServicioMapeoReunion servicioMapeo, IConfiguration configuracion, ILogger<WebhooksReunionesControlador> logger)
    { _servicioSincronizacion = servicioSincronizacion; _servicioMapeo = servicioMapeo; _configuracion = configuracion; _logger = logger; }

    /// <summary>Intenta sincronizar; si no hay un MapeoOrigenReunion configurado para
    /// ese token de correlación, registra una advertencia y no falla — el webhook
    /// siempre responde 200 para que la plataforma no reintente indefinidamente.</summary>
    private async Task IntentarSincronizarAsync(ProveedorReunion proveedor, string tokenCorrelacion, string idReunionExterna)
    {
        var mapeo = await _servicioMapeo.ResolverAsync(proveedor, tokenCorrelacion);
        if (mapeo is null)
        {
            _logger.LogWarning(
                "Webhook de {Proveedor} sin mapeo configurado para el token {Token} — reunión {IdReunion} no sincronizada. " +
                "Créalo en POST /api/mapeos-reunion.", proveedor, tokenCorrelacion, idReunionExterna);
            return;
        }

        await _servicioSincronizacion.SincronizarReunionAsync(
            proveedor, idReunionExterna, mapeo.IdEmpresa, mapeo.IdSede, TipoActa.Reunion, mapeo.IdUsuarioResponsable);
    }

    /// <summary>Zoom envía un reto de validación al registrar el endpoint, y luego
    /// eventos reales firmados con el header "x-zm-signature".</summary>
    [HttpPost("zoom")]
    public async Task<IActionResult> WebhookZoom([FromBody] System.Text.Json.JsonElement carga)
    {
        if (carga.TryGetProperty("event", out var evento) && evento.GetString() == "endpoint.url_validation")
        {
            // Zoom exige responder un hash HMAC del "plainToken" recibido para confirmar el endpoint.
            var tokenPlano = carga.GetProperty("payload").GetProperty("plainToken").GetString();
            var secreto = _configuracion["Integraciones:Zoom:TokenSecretoWebhook"]!;
            var hash = Convert.ToHexString(
                new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secreto))
                    .ComputeHash(System.Text.Encoding.UTF8.GetBytes(tokenPlano!))).ToLower();
            return Ok(new { plainToken = tokenPlano, encryptedToken = hash });
        }

        // TODO producción: validar la firma "x-zm-signature" (HMAC-SHA256 sobre
        // "v0:{timestamp}:{cuerpo}" con el mismo secreto de arriba) antes de confiar
        // en el evento — implementado ya para Webex (ver abajo); aplica el mismo
        // patrón acá antes de ir a producción con Zoom.
        if (carga.TryGetProperty("event", out var tipoEvento) && tipoEvento.GetString() == "meeting.ended"
            && carga.TryGetProperty("payload", out var payload) && payload.TryGetProperty("object", out var objeto))
        {
            // Zoom no ofrece un campo de correlación libre como Teams/GoogleMeet —
            // se usa el correo del anfitrión de la reunión como token de mapeo.
            var idReunionExterna = objeto.GetProperty("id").GetRawText();
            var correoAnfitrion = objeto.TryGetProperty("host_email", out var h) ? h.GetString() : null;
            if (!string.IsNullOrEmpty(correoAnfitrion))
                await IntentarSincronizarAsync(ProveedorReunion.Zoom, correoAnfitrion, idReunionExterna);
        }
        return Ok();
    }

    /// <summary>Microsoft Graph exige responder el "validationToken" en texto plano al
    /// registrar la suscripción de notificaciones de cambio. Cada notificación trae de
    /// vuelta el "clientState" que se eligió al crear esa suscripción — es el token de
    /// correlación (ver MapeoOrigenReunion): crea una suscripción distinta por cada
    /// empresa/organizador y usa un clientState único para cada una.</summary>
    [HttpPost("teams")]
    public async Task<IActionResult> WebhookTeams([FromQuery] string? validationToken)
    {
        if (!string.IsNullOrEmpty(validationToken)) return Content(validationToken, "text/plain");

        Request.EnableBuffering();
        using var lector = new StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
        var cuerpo = await lector.ReadToEndAsync();
        Request.Body.Position = 0;

        // TODO producción: Graph también permite validar la autenticidad del payload
        // completo por token de validación adicional en el header "Authorization" de
        // la suscripción — más allá del alcance de esta demo, ver la documentación de
        // Microsoft Graph sobre "Validate the origin of notifications".
        var carga = System.Text.Json.JsonDocument.Parse(cuerpo).RootElement;
        if (!carga.TryGetProperty("value", out var notificaciones)) return Ok();

        foreach (var notificacion in notificaciones.EnumerateArray())
        {
            var clientState = notificacion.TryGetProperty("clientState", out var cs) ? cs.GetString() : null;
            var idReunionExterna = notificacion.TryGetProperty("resourceData", out var rd) && rd.TryGetProperty("id", out var id)
                ? id.GetString() : null;
            if (!string.IsNullOrEmpty(clientState) && !string.IsNullOrEmpty(idReunionExterna))
                await IntentarSincronizarAsync(ProveedorReunion.Teams, clientState, idReunionExterna);
        }
        return Ok();
    }

    /// <summary>Google Workspace usa Calendar push notifications: el header
    /// "X-Goog-Channel-Token" (elegido al crear el canal de observación) sirve como
    /// token de correlación, igual que el clientState de Teams. Limitación real de
    /// la plataforma: la notificación en sí NO trae el id de la reunión que cambió
    /// — solo avisa "algo cambió en este calendario", y toca llamar después a la API
    /// de Calendar (events.list con el syncToken guardado) para averiguar qué fue.
    /// Esa segunda llamada queda fuera de este alcance — aquí solo se resuelve la
    /// empresa/sede del canal, quedando lista para conectarla con esa consulta.</summary>
    [HttpPost("google-meet")]
    public async Task<IActionResult> WebhookGoogleMeet()
    {
        var tokenCanal = Request.Headers["X-Goog-Channel-Token"].FirstOrDefault();
        var estadoRecurso = Request.Headers["X-Goog-Resource-State"].FirstOrDefault();

        if (string.IsNullOrEmpty(tokenCanal) || estadoRecurso == "sync")
            return Ok(); // "sync" es solo la confirmación inicial del canal, no un cambio real.

        var mapeo = await _servicioMapeo.ResolverAsync(ProveedorReunion.GoogleMeet, tokenCanal);
        if (mapeo is null)
        {
            _logger.LogWarning("Webhook de GoogleMeet sin mapeo configurado para el canal {Token}.", tokenCanal);
            return Ok();
        }

        // TODO producción: con el mapeo ya resuelto, llamar a Calendar API
        // (events.list, con el syncToken guardado para este canal) para obtener el
        // id de la reunión recién finalizada, y entonces sí:
        // await _servicioSincronizacion.SincronizarReunionAsync(ProveedorReunion.GoogleMeet,
        //     idReunionExterna, mapeo.IdEmpresa, mapeo.IdSede, TipoActa.Reunion, mapeo.IdUsuarioResponsable);
        _logger.LogInformation("Cambio detectado para {Empresa} vía GoogleMeet — pendiente resolver el id de reunión vía Calendar API.", mapeo.NombreEmpresa);
        return Ok();
    }

    /// <summary>Webex firma cada evento con HMAC-SHA1 en el header "X-Spark-Signature"
    /// (sobre el cuerpo crudo de la petición) — a diferencia de Zoom/Teams, no exige un
    /// reto de validación al registrar el webhook: eso se resuelve al crearlo vía
    /// POST /v1/webhooks en la API de Webex, pasando este mismo targetUrl y secret.
    /// Valida la firma con comparación en tiempo constante antes de confiar en el
    /// evento — sin esto, cualquiera que descubra esta URL podría inventar eventos
    /// falsos de "reunión terminada". El correo del anfitrión (sin campo de
    /// correlación libre, igual que Zoom) sirve como token de mapeo.</summary>
    [HttpPost("webex")]
    public async Task<IActionResult> WebhookWebex()
    {
        Request.EnableBuffering();
        string cuerpoCrudo;
        using (var lector = new StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true))
            cuerpoCrudo = await lector.ReadToEndAsync();
        Request.Body.Position = 0;

        var secreto = _configuracion["Integraciones:Webex:TokenSecretoWebhook"];
        var firmaRecibida = Request.Headers["X-Spark-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(secreto) || string.IsNullOrEmpty(firmaRecibida))
            return Unauthorized();

        var firmaCalculada = Convert.ToHexString(
            new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes(secreto))
                .ComputeHash(System.Text.Encoding.UTF8.GetBytes(cuerpoCrudo))).ToLowerInvariant();

        var coinciden = System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(firmaCalculada),
            System.Text.Encoding.UTF8.GetBytes(firmaRecibida.ToLowerInvariant()));
        if (!coinciden) return Unauthorized();

        var carga = System.Text.Json.JsonDocument.Parse(cuerpoCrudo).RootElement;
        var esReunionTerminada = carga.TryGetProperty("resource", out var recurso) && recurso.GetString() == "meetings"
            && carga.TryGetProperty("event", out var evento) && evento.GetString() == "ended";

        if (esReunionTerminada && carga.TryGetProperty("data", out var datos))
        {
            var idReunionExterna = datos.TryGetProperty("id", out var id) ? id.GetString() : null;
            var correoAnfitrion = datos.TryGetProperty("hostEmail", out var h) ? h.GetString() : null;
            if (!string.IsNullOrEmpty(correoAnfitrion) && !string.IsNullOrEmpty(idReunionExterna))
                await IntentarSincronizarAsync(ProveedorReunion.Webex, correoAnfitrion, idReunionExterna);
        }
        return Ok();
    }
}
