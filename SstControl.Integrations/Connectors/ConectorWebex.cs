using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SstControl.Aplicacion.Integraciones;
using SstControl.Integraciones.Autenticacion;

namespace SstControl.Integraciones.Conectores;

/// <summary>
/// Conector a nivel developer para Cisco Webex, vía OAuth2 vía una "Service App"
/// (acceso de organización completa, no ligado a un usuario individual — el
/// equivalente de Webex al app-only auth de Teams o al Server-to-Server OAuth de Zoom).
///
/// Registro requerido en developer.webex.com:
///   - Crear una "Service App" y solicitar su activación en la organización (admin).
///   - Scopes: meeting:schedules_read, meeting:participants_read, meeting:recordings_read,
///     meeting:transcripts_read (o el subconjunto que necesites).
///   - Webex entrega un client_id/client_secret + un refresh_token de larga duración;
///     cada access_token se obtiene bajo demanda a partir de ese refresh_token.
///
/// Endpoints reales usados:
///   Token:          POST https://webexapis.com/v1/access_token  (grant_type=refresh_token)
///   Reunión:        GET  https://webexapis.com/v1/meetings/{idReunion}
///   Participantes:  GET  https://webexapis.com/v1/meetingParticipants?meetingId={idReunion}
///   Transcripciones: GET https://webexapis.com/v1/meetingTranscripts?meetingId={idReunion}
///                    GET https://webexapis.com/v1/meetingTranscripts/{idTranscripcion}/download?format=txt
///   Grabaciones:    GET  https://webexapis.com/v1/recordings?meetingId={idReunion} (respaldo si no hay transcripción)
/// </summary>
public class ConectorWebex : IConectorReunion
{
    public ProveedorReunion Proveedor => ProveedorReunion.Webex;

    private readonly HttpClient _http;
    private readonly ProveedorTokenClientCredentials _proveedorToken;
    private readonly IConfiguration _configuracion;

    public ConectorWebex(HttpClient http, IConfiguration configuracion)
    {
        _http = http;
        _proveedorToken = new ProveedorTokenClientCredentials(http);
        _configuracion = configuracion;
    }

    /// <summary>A diferencia de Teams/Zoom (client credentials puro), la Service App de
    /// Webex intercambia un refresh_token de larga duración por access_tokens de corta
    /// duración — por eso el refresh_token también viaja en el cuerpo del formulario.</summary>
    private Task<string> ObtenerTokenAsync()
    {
        var idCliente = _configuracion["Integraciones:Webex:IdCliente"];
        var claveCliente = _configuracion["Integraciones:Webex:ClaveCliente"];
        var tokenRenovacion = _configuracion["Integraciones:Webex:TokenRenovacion"];

        return _proveedorToken.ObtenerTokenAsync("https://webexapis.com/v1/access_token", new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = idCliente!,
            ["client_secret"] = claveCliente!,
            ["refresh_token"] = tokenRenovacion!,
        });
    }

    private async Task<JsonElement> ConsultarWebexAsync(string url)
    {
        var token = await ObtenerTokenAsync();
        using var peticion = new HttpRequestMessage(HttpMethod.Get, url);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await _http.SendAsync(peticion);
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    public async Task<ReunionExternaDto> ObtenerReunionAsync(string idReunionExterna)
    {
        var json = await ConsultarWebexAsync($"https://webexapis.com/v1/meetings/{idReunionExterna}");
        return new ReunionExternaDto(
            IdReunionExterna: idReunionExterna,
            Titulo: json.TryGetProperty("title", out var titulo) ? titulo.GetString() ?? "Reunión de Webex" : "Reunión de Webex",
            FechaInicio: DateTimeOffset.Parse(json.GetProperty("start").GetString()!),
            FechaFin: json.TryGetProperty("end", out var fin) && fin.ValueKind == JsonValueKind.String
                ? DateTimeOffset.Parse(fin.GetString()!) : null,
            UrlIngreso: json.TryGetProperty("webLink", out var enlace) ? enlace.GetString() : null);
    }

    /// <summary>Trae los participantes reales de una reunión ya finalizada.</summary>
    public async Task<IReadOnlyList<AsistenteExternoDto>> ObtenerAsistenciaAsync(string idReunionExterna)
    {
        var json = await ConsultarWebexAsync($"https://webexapis.com/v1/meetingParticipants?meetingId={idReunionExterna}");
        var lista = new List<AsistenteExternoDto>();
        if (!json.TryGetProperty("items", out var items)) return lista;

        foreach (var participante in items.EnumerateArray())
        {
            lista.Add(new AsistenteExternoDto(
                Nombre: participante.TryGetProperty("displayName", out var nombre) ? nombre.GetString() ?? "Desconocido" : "Desconocido",
                CorreoElectronico: participante.TryGetProperty("email", out var correo) ? correo.GetString() : null,
                HoraIngreso: participante.TryGetProperty("joinedTime", out var ingreso) && ingreso.ValueKind == JsonValueKind.String
                    ? DateTimeOffset.Parse(ingreso.GetString()!) : null,
                HoraSalida: participante.TryGetProperty("leftTime", out var salida) && salida.ValueKind == JsonValueKind.String
                    ? DateTimeOffset.Parse(salida.GetString()!) : null));
        }
        return lista;
    }

    /// <summary>Prioriza la transcripción (texto, insumo real para el bot de minutas —
    /// ver IServicioBotActas) y, si no hay ninguna disponible, cae al enlace de la
    /// grabación como contenido de respaldo.</summary>
    public async Task<ContenidoExternoDto?> ObtenerContenidoAsync(string idReunionExterna)
    {
        try
        {
            var transcripciones = await ConsultarWebexAsync($"https://webexapis.com/v1/meetingTranscripts?meetingId={idReunionExterna}");
            if (transcripciones.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                var idTranscripcion = items[0].GetProperty("id").GetString();
                var token = await ObtenerTokenAsync();
                using var peticion = new HttpRequestMessage(HttpMethod.Get,
                    $"https://webexapis.com/v1/meetingTranscripts/{idTranscripcion}/download?format=txt");
                peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var respuesta = await _http.SendAsync(peticion);
                if (respuesta.IsSuccessStatusCode)
                {
                    var texto = await respuesta.Content.ReadAsStringAsync();
                    return new ContenidoExternoDto(Resumen: texto, UrlGrabacion: null, TipoContenido: "transcript");
                }
            }
        }
        catch (HttpRequestException) { /* sin transcripción disponible — se intenta con la grabación */ }

        try
        {
            var grabaciones = await ConsultarWebexAsync($"https://webexapis.com/v1/recordings?meetingId={idReunionExterna}");
            if (grabaciones.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
            {
                var urlReproduccion = items[0].TryGetProperty("playbackUrl", out var url) ? url.GetString() : null;
                if (urlReproduccion != null)
                    return new ContenidoExternoDto(Resumen: null, UrlGrabacion: urlReproduccion, TipoContenido: "recording");
            }
        }
        catch (HttpRequestException) { /* sin grabación tampoco: se interpreta como "sin contenido" */ }

        return null;
    }
}
