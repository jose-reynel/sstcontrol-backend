using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SstControl.Aplicacion.Integraciones;
using SstControl.Integraciones.Autenticacion;

namespace SstControl.Integraciones.Conectores;

/// <summary>
/// Conector a nivel developer para Microsoft Teams, vía Microsoft Graph.
///
/// Registro requerido en Azure AD (portal.azure.com → App registrations):
///   - Crear la app, generar un Client Secret.
///   - Permisos de API de tipo Application (no delegados, requieren consentimiento de admin):
///       OnlineMeetings.Read.All
///       OnlineMeetingArtifact.Read.All  (para asistencia y transcripciones)
///
/// Endpoints reales usados:
///   Token:      POST https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token
///   Reunión:    GET  https://graph.microsoft.com/v1.0/users/{idOrganizador}/onlineMeetings/{idReunion}
///   Asistencia: GET  .../onlineMeetings/{idReunion}/attendanceReports
///               GET  .../attendanceReports/{idReporte}/attendanceRecords
/// </summary>
public class ConectorTeams : IConectorReunion
{
    public ProveedorReunion Proveedor => ProveedorReunion.Teams;

    private readonly HttpClient _http;
    private readonly ProveedorTokenClientCredentials _proveedorToken;
    private readonly IConfiguration _configuracion;

    public ConectorTeams(HttpClient http, IConfiguration configuracion)
    {
        _http = http;
        _proveedorToken = new ProveedorTokenClientCredentials(http);
        _configuracion = configuracion;
    }

    /// <summary>Solicita (o reutiliza) el token de aplicación de Azure AD para Microsoft Graph.</summary>
    private async Task<string> ObtenerTokenAsync()
    {
        var idInquilino = _configuracion["Integraciones:Teams:IdInquilino"];
        var idCliente = _configuracion["Integraciones:Teams:IdCliente"];
        var claveCliente = _configuracion["Integraciones:Teams:ClaveCliente"];
        return await _proveedorToken.ObtenerTokenAsync(
            $"https://login.microsoftonline.com/{idInquilino}/oauth2/v2.0/token",
            new Dictionary<string, string>
            {
                ["client_id"] = idCliente!,
                ["client_secret"] = claveCliente!,
                ["scope"] = "https://graph.microsoft.com/.default",
                ["grant_type"] = "client_credentials",
            });
    }

    private async Task<HttpResponseMessage> ConsultarGraphAsync(string url)
    {
        var token = await ObtenerTokenAsync();
        using var peticion = new HttpRequestMessage(HttpMethod.Get, url);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await _http.SendAsync(peticion);
        respuesta.EnsureSuccessStatusCode();
        return respuesta;
    }

    /// <summary>Trae los datos generales de la reunión (tema, horario, enlace de ingreso).</summary>
    public async Task<ReunionExternaDto> ObtenerReunionAsync(string idReunionExterna)
    {
        var idOrganizador = _configuracion["Integraciones:Teams:IdUsuarioOrganizador"];
        var respuesta = await ConsultarGraphAsync($"https://graph.microsoft.com/v1.0/users/{idOrganizador}/onlineMeetings/{idReunionExterna}");
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        return new ReunionExternaDto(
            IdReunionExterna: idReunionExterna,
            Titulo: json.GetProperty("subject").GetString() ?? "Reunión de Teams",
            FechaInicio: DateTimeOffset.Parse(json.GetProperty("startDateTime").GetString()!),
            FechaFin: json.TryGetProperty("endDateTime", out var fin) ? DateTimeOffset.Parse(fin.GetString()!) : null,
            UrlIngreso: json.TryGetProperty("joinWebUrl", out var url) ? url.GetString() : null);
    }

    /// <summary>Trae la lista de asistentes reales con sus intervalos de entrada/salida
    /// desde el reporte de asistencia más reciente de la reunión.</summary>
    public async Task<IReadOnlyList<AsistenteExternoDto>> ObtenerAsistenciaAsync(string idReunionExterna)
    {
        var idOrganizador = _configuracion["Integraciones:Teams:IdUsuarioOrganizador"];
        var respuestaReportes = await ConsultarGraphAsync(
            $"https://graph.microsoft.com/v1.0/users/{idOrganizador}/onlineMeetings/{idReunionExterna}/attendanceReports");
        var jsonReportes = await respuestaReportes.Content.ReadFromJsonAsync<JsonElement>();
        var reportes = jsonReportes.GetProperty("value");
        if (reportes.GetArrayLength() == 0) return Array.Empty<AsistenteExternoDto>();

        var idUltimoReporte = reportes[0].GetProperty("id").GetString();
        var respuestaRegistros = await ConsultarGraphAsync(
            $"https://graph.microsoft.com/v1.0/users/{idOrganizador}/onlineMeetings/{idReunionExterna}/attendanceReports/{idUltimoReporte}/attendanceRecords");
        var jsonRegistros = await respuestaRegistros.Content.ReadFromJsonAsync<JsonElement>();

        var lista = new List<AsistenteExternoDto>();
        foreach (var registro in jsonRegistros.GetProperty("value").EnumerateArray())
        {
            var intervalos = registro.GetProperty("attendanceIntervals").EnumerateArray().ToList();
            DateTimeOffset? ingreso = intervalos.Count > 0 ? DateTimeOffset.Parse(intervalos.First().GetProperty("joinDateTime").GetString()!) : null;
            DateTimeOffset? salida = intervalos.Count > 0 ? DateTimeOffset.Parse(intervalos.Last().GetProperty("leaveDateTime").GetString()!) : null;
            lista.Add(new AsistenteExternoDto(
                Nombre: registro.GetProperty("identity").GetProperty("displayName").GetString() ?? "Desconocido",
                CorreoElectronico: registro.TryGetProperty("emailAddress", out var correo) ? correo.GetString() : null,
                HoraIngreso: ingreso, HoraSalida: salida));
        }
        return lista;
    }

    public Task<ContenidoExternoDto?> ObtenerContenidoAsync(string idReunionExterna)
    {
        // La transcripción vive en /onlineMeetings/{id}/transcripts (requiere el permiso
        // OnlineMeetingTranscript.Read.All) — se deja como extensión futura.
        return Task.FromResult<ContenidoExternoDto?>(null);
    }
}
