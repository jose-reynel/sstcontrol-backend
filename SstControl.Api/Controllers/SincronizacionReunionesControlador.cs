using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IConfiguration _configuracion;
    public WebhooksReunionesControlador(IServicioSincronizacionReuniones servicioSincronizacion, IConfiguration configuracion)
    { _servicioSincronizacion = servicioSincronizacion; _configuracion = configuracion; }

    /// <summary>Zoom envía un reto de validación al registrar el endpoint, y luego
    /// eventos reales firmados con el header "x-zm-signature".</summary>
    [HttpPost("zoom")]
    public IActionResult WebhookZoom([FromBody] System.Text.Json.JsonElement carga)
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

        if (carga.TryGetProperty("event", out var tipoEvento) && tipoEvento.GetString() == "meeting.ended")
        {
            // TODO producción: validar la firma x-zm-signature antes de procesar, y resolver
            // la empresa/sede según la convención definida (ver Manual de Parametrización).
        }
        return Ok();
    }

    /// <summary>Microsoft Graph exige responder el "validationToken" en texto plano al
    /// registrar la suscripción de notificaciones de cambio.</summary>
    [HttpPost("teams")]
    public IActionResult WebhookTeams([FromQuery] string? validationToken)
    {
        if (!string.IsNullOrEmpty(validationToken)) return Content(validationToken, "text/plain");
        // TODO producción: leer el cuerpo con las notificaciones de reuniones finalizadas
        // y disparar _servicioSincronizacion.SincronizarReunionAsync(...) para cada una.
        return Ok();
    }

    /// <summary>Google Workspace usa Pub/Sub (no un webhook HTTP directo) para eventos de
    /// Meet — este endpoint queda como receptor si se configura un push subscription a HTTP.</summary>
    [HttpPost("google-meet")]
    public IActionResult WebhookGoogleMeet() => Ok();

    /// <summary>Webex firma cada evento con HMAC-SHA1 en el header "X-Spark-Signature"
    /// (sobre el cuerpo crudo de la petición) — a diferencia de Zoom/Teams, no exige un
    /// reto de validación al registrar el webhook: eso se resuelve al crearlo vía
    /// POST /v1/webhooks en la API de Webex, pasando este mismo targetUrl y secret.
    /// Valida la firma con comparación en tiempo constante antes de confiar en el
    /// evento — sin esto, cualquiera que descubra esta URL podría inventar eventos
    /// falsos de "reunión terminada".</summary>
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

        if (esReunionTerminada)
        {
            // TODO producción: resolver la empresa/sede según la convención definida
            // (ver Manual de Parametrización) y disparar
            // _servicioSincronizacion.SincronizarReunionAsync(ProveedorReunion.Webex, ...).
        }
        return Ok();
    }
}
