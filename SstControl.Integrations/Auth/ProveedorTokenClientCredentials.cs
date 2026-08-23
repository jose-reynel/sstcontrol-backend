using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SstControl.Integraciones.Autenticacion;

/// <summary>
/// Ayudante compartido para flujos OAuth2 "client credentials" (Teams vía Azure AD,
/// Zoom vía Server-to-Server OAuth). Guarda el token en memoria hasta que expira,
/// para no solicitar uno nuevo en cada llamada.
/// </summary>
public class ProveedorTokenClientCredentials
{
    private readonly HttpClient _http;
    private string? _tokenEnCache;
    private DateTimeOffset _expiraEn = DateTimeOffset.MinValue;

    public ProveedorTokenClientCredentials(HttpClient http) => _http = http;

    /// <summary>Obtiene un token de acceso, reutilizando el que tiene en caché si aún es válido.</summary>
    public async Task<string> ObtenerTokenAsync(string urlToken, Dictionary<string, string> cuerpoFormulario, AuthenticationHeaderValue? autenticacionBasica = null)
    {
        if (_tokenEnCache != null && DateTimeOffset.UtcNow < _expiraEn.AddSeconds(-30))
            return _tokenEnCache;

        using var peticion = new HttpRequestMessage(HttpMethod.Post, urlToken) { Content = new FormUrlEncodedContent(cuerpoFormulario) };
        if (autenticacionBasica != null) peticion.Headers.Authorization = autenticacionBasica;

        var respuesta = await _http.SendAsync(peticion);
        respuesta.EnsureSuccessStatusCode();
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();

        _tokenEnCache = json.GetProperty("access_token").GetString()!;
        var segundosExpiracion = json.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
        _expiraEn = DateTimeOffset.UtcNow.AddSeconds(segundosExpiracion);
        return _tokenEnCache;
    }
}
