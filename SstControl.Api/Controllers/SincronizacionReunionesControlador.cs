using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.Integraciones;
using SstControl.Dominio.Entidades;

namespace SstControl.Api.Controladores;

/// <summary>Datos para sincronizar manualmente una reunión externa hacia el sistema central.</summary>
public record PeticionSincronizarReunion(string IdReunionExterna, int IdEmpresa, int IdSede, string Tipo);

/// <summary>Sincronización on-demand de reuniones desde Teams, Google Meet o Zoom (solo administrador).</summary>
[ApiController]
[Authorize(Roles = "admin")]
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
            return BadRequest(new { mensaje = "Proveedor no soportado. Usa teams, googlemeet o zoom." });

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
}
