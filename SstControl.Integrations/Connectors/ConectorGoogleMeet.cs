using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SstControl.Aplicacion.Integraciones;

namespace SstControl.Integraciones.Conectores;

/// <summary>
/// Conector a nivel developer para Google Meet, vía la Google Meet REST API
/// (meet.googleapis.com), liberada por Google en 2023.
///
/// Registro requerido en Google Cloud Console:
///   - Crear proyecto, habilitar "Google Meet API".
///   - Crear una cuenta de servicio, descargar su clave privada (JSON).
///   - En Google Workspace Admin, autorizar esa cuenta de servicio con
///     "Domain-wide delegation" y el scope:
///     https://www.googleapis.com/auth/meetings.space.readonly
///
/// Endpoints reales usados:
///   Token:         POST https://oauth2.googleapis.com/token (grant_type=jwt-bearer, firmado RS256)
///   Registro:      GET  https://meet.googleapis.com/v2/conferenceRecords/{idRegistro}
///   Participantes: GET  https://meet.googleapis.com/v2/{idRegistro}/participants
/// </summary>
public class ConectorGoogleMeet : IConectorReunion
{
    public ProveedorReunion Proveedor => ProveedorReunion.GoogleMeet;

    private readonly HttpClient _http;
    private readonly IConfiguration _configuracion;
    private string? _tokenEnCache;
    private DateTimeOffset _expiraEn = DateTimeOffset.MinValue;

    public ConectorGoogleMeet(HttpClient http, IConfiguration configuracion)
    {
        _http = http;
        _configuracion = configuracion;
    }

    /// <summary>Construye y firma el JWT de autorización de la cuenta de servicio
    /// (RFC 7523) y lo intercambia por un token de acceso en el endpoint de Google.</summary>
    private async Task<string> ObtenerTokenAsync()
    {
        if (_tokenEnCache != null && DateTimeOffset.UtcNow < _expiraEn.AddSeconds(-30))
            return _tokenEnCache;

        var correoServicio = _configuracion["Integraciones:GoogleMeet:CorreoCuentaServicio"]!;
        var clavePrivadaPem = _configuracion["Integraciones:GoogleMeet:ClavePrivada"]!;
        var usuarioSuplantado = _configuracion["Integraciones:GoogleMeet:CorreoUsuarioSuplantado"]; // domain-wide delegation

        var ahora = DateTimeOffset.UtcNow;
        var encabezado = CodificarBase64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT" }));
        var reclamos = new Dictionary<string, object>
        {
            ["iss"] = correoServicio,
            ["scope"] = "https://www.googleapis.com/auth/meetings.space.readonly",
            ["aud"] = "https://oauth2.googleapis.com/token",
            ["iat"] = ahora.ToUnixTimeSeconds(),
            ["exp"] = ahora.AddMinutes(50).ToUnixTimeSeconds(),
        };
        if (!string.IsNullOrEmpty(usuarioSuplantado)) reclamos["sub"] = usuarioSuplantado;
        var carga = CodificarBase64Url(JsonSerializer.SerializeToUtf8Bytes(reclamos));

        var sinFirmar = $"{encabezado}.{carga}";
        using var rsa = RSA.Create();
        rsa.ImportFromPem(clavePrivadaPem);
        var firma = rsa.SignData(Encoding.UTF8.GetBytes(sinFirmar), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var jwt = $"{sinFirmar}.{CodificarBase64Url(firma)}";

        using var peticion = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = jwt,
            }),
        };
        var respuesta = await _http.SendAsync(peticion);
        respuesta.EnsureSuccessStatusCode();
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        _tokenEnCache = json.GetProperty("access_token").GetString()!;
        _expiraEn = DateTimeOffset.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32());
        return _tokenEnCache;
    }

    private static string CodificarBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private async Task<JsonElement> ConsultarMeetAsync(string url)
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
        var json = await ConsultarMeetAsync($"https://meet.googleapis.com/v2/conferenceRecords/{idReunionExterna}");
        return new ReunionExternaDto(
            IdReunionExterna: idReunionExterna,
            Titulo: "Reunión de Google Meet", // el nombre del espacio no siempre viaja en el conferenceRecord
            FechaInicio: DateTimeOffset.Parse(json.GetProperty("startTime").GetString()!),
            FechaFin: json.TryGetProperty("endTime", out var fin) ? DateTimeOffset.Parse(fin.GetString()!) : null,
            UrlIngreso: null);
    }

    public async Task<IReadOnlyList<AsistenteExternoDto>> ObtenerAsistenciaAsync(string idReunionExterna)
    {
        var json = await ConsultarMeetAsync($"https://meet.googleapis.com/v2/conferenceRecords/{idReunionExterna}/participants?pageSize=250");
        var lista = new List<AsistenteExternoDto>();
        if (!json.TryGetProperty("participants", out var participantes)) return lista;

        foreach (var participante in participantes.EnumerateArray())
        {
            var nombre = participante.TryGetProperty("signedinUser", out var su) && su.TryGetProperty("displayName", out var dn)
                ? dn.GetString() : "Invitado";
            lista.Add(new AsistenteExternoDto(
                Nombre: nombre ?? "Invitado",
                CorreoElectronico: null, // requiere el scope admin.reports.audit, no incluido por defecto
                HoraIngreso: participante.TryGetProperty("earliestStartTime", out var ingreso) ? DateTimeOffset.Parse(ingreso.GetString()!) : null,
                HoraSalida: participante.TryGetProperty("latestEndTime", out var salida) ? DateTimeOffset.Parse(salida.GetString()!) : null));
        }
        return lista;
    }

    public Task<ContenidoExternoDto?> ObtenerContenidoAsync(string idReunionExterna)
    {
        // Requiere el scope meetings.space.readonly + grabaciones habilitadas en el espacio.
        // Se deja como extensión futura (GET .../recordings y .../transcripts).
        return Task.FromResult<ContenidoExternoDto?>(null);
    }
}
