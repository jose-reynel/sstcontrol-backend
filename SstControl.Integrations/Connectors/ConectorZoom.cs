using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SstControl.Aplicacion.Integraciones;
using SstControl.Integraciones.Autenticacion;

namespace SstControl.Integraciones.Conectores;

/// <summary>
/// Conector a nivel developer para Zoom, vía Server-to-Server OAuth (reemplazo oficial
/// de JWT, que Zoom descontinuó).
///
/// Registro requerido en marketplace.zoom.us:
///   - Crear una app tipo "Server-to-Server OAuth".
///   - Scopes: meeting:read:admin, report:read:admin (y recording:read:admin para grabaciones).
///
/// Endpoints reales usados:
///   Token:         POST https://zoom.us/oauth/token?grant_type=account_credentials&amp;account_id={idCuenta}
///   Reunión:       GET  https://api.zoom.us/v2/meetings/{idReunion}
///   Participantes: GET  https://api.zoom.us/v2/past_meetings/{idReunion}/participants
/// </summary>
public class ConectorZoom : IConectorReunion
{
    public ProveedorReunion Proveedor => ProveedorReunion.Zoom;

    private readonly HttpClient _http;
    private readonly ProveedorTokenClientCredentials _proveedorToken;
    private readonly IConfiguration _configuracion;

    public ConectorZoom(HttpClient http, IConfiguration configuracion)
    {
        _http = http;
        _proveedorToken = new ProveedorTokenClientCredentials(http);
        _configuracion = configuracion;
    }

    private async Task<string> ObtenerTokenAsync()
    {
        var idCuenta = _configuracion["Integraciones:Zoom:IdCuenta"];
        var idCliente = _configuracion["Integraciones:Zoom:IdCliente"];
        var claveCliente = _configuracion["Integraciones:Zoom:ClaveCliente"];
        var basica = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{idCliente}:{claveCliente}")));

        return await _proveedorToken.ObtenerTokenAsync(
            $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={idCuenta}",
            new Dictionary<string, string>(), basica);
    }

    private async Task<JsonElement> ConsultarZoomAsync(string url)
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
        var json = await ConsultarZoomAsync($"https://api.zoom.us/v2/meetings/{idReunionExterna}");
        return new ReunionExternaDto(
            IdReunionExterna: idReunionExterna,
            Titulo: json.GetProperty("topic").GetString() ?? "Reunión de Zoom",
            FechaInicio: DateTimeOffset.Parse(json.GetProperty("start_time").GetString()!),
            FechaFin: null,
            UrlIngreso: json.TryGetProperty("join_url", out var url) ? url.GetString() : null);
    }

    /// <summary>Trae los participantes reales de una reunión ya finalizada.</summary>
    public async Task<IReadOnlyList<AsistenteExternoDto>> ObtenerAsistenciaAsync(string idReunionExterna)
    {
        var json = await ConsultarZoomAsync($"https://api.zoom.us/v2/past_meetings/{idReunionExterna}/participants?page_size=300");
        var lista = new List<AsistenteExternoDto>();
        foreach (var participante in json.GetProperty("participants").EnumerateArray())
        {
            lista.Add(new AsistenteExternoDto(
                Nombre: participante.GetProperty("name").GetString() ?? "Desconocido",
                CorreoElectronico: participante.TryGetProperty("user_email", out var correo) ? correo.GetString() : null,
                HoraIngreso: participante.TryGetProperty("join_time", out var ingreso) ? DateTimeOffset.Parse(ingreso.GetString()!) : null,
                HoraSalida: participante.TryGetProperty("leave_time", out var salida) ? DateTimeOffset.Parse(salida.GetString()!) : null));
        }
        return lista;
    }

    public async Task<ContenidoExternoDto?> ObtenerContenidoAsync(string idReunionExterna)
    {
        // Requiere el scope adicional recording:read:admin. Si la reunión no fue
        // grabada, Zoom responde con error y se interpreta como "sin contenido".
        try
        {
            var json = await ConsultarZoomAsync($"https://api.zoom.us/v2/meetings/{idReunionExterna}/recordings");
            var archivos = json.GetProperty("recording_files").EnumerateArray().ToList();
            var urlCompartir = json.TryGetProperty("share_url", out var su) ? su.GetString() : null;
            if (archivos.Count == 0 && urlCompartir == null) return null;
            return new ContenidoExternoDto(Resumen: null, UrlGrabacion: urlCompartir, TipoContenido: "recording");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}
