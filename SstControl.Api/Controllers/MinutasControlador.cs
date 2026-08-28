using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>
/// El "bot de minutas": da seguimiento a una Acta más allá del registro de la
/// reunión en sí — genera compromisos a partir de su contenido ya sincronizado y
/// permite vincularlos al cambio documental que los cierra. Usa los mismos
/// permisos que ya rigen Actas ("actas.ver" / "actas.crear").
/// </summary>
[ApiController]
[Authorize]
[Route("api/actas/{idActa:int}")]
public class MinutasControlador : ControllerBase
{
    private readonly IServicioBotActas _bot;
    public MinutasControlador(IServicioBotActas bot) => _bot = bot;

    /// <summary>POST /api/actas/{idActa}/generar-minuta — corre el bot sobre el
    /// contenido ya sincronizado del acta (transcripción/resumen) y registra los
    /// compromisos nuevos que detecte. Seguro de llamar varias veces: no duplica
    /// los compromisos que el bot ya había generado antes sobre el mismo texto.</summary>
    [HttpPost("generar-minuta")]
    [Authorize(Policy = "actas.crear")]
    public async Task<ActionResult<MinutaGeneradaDto>> GenerarMinuta(int idActa) =>
        Ok(await _bot.GenerarMinutaAsync(idActa));

    /// <summary>GET /api/actas/{idActa}/compromisos — lista los compromisos de seguimiento del acta.</summary>
    [HttpGet("compromisos")]
    [Authorize(Policy = "actas.ver")]
    public async Task<ActionResult<IReadOnlyList<CompromisoActaDto>>> ObtenerCompromisos(int idActa) =>
        Ok(await _bot.ObtenerCompromisosAsync(idActa));

    /// <summary>POST /api/actas/{idActa}/compromisos — agrega un compromiso a mano
    /// (el bot genera los suyos automáticamente vía generar-minuta).</summary>
    [HttpPost("compromisos")]
    [Authorize(Policy = "actas.crear")]
    public async Task<ActionResult<CompromisoActaDto>> AgregarCompromiso(int idActa, CrearCompromisoDto datos) =>
        Ok(await _bot.AgregarCompromisoAsync(idActa, datos));
}

/// <summary>Acciones sobre un compromiso puntual — ruta propia porque no dependen
/// de conocer el acta a la que pertenece, solo su propio id.</summary>
[ApiController]
[Authorize]
[Route("api/compromisos")]
public class CompromisosControlador : ControllerBase
{
    private readonly IServicioBotActas _bot;
    public CompromisosControlador(IServicioBotActas bot) => _bot = bot;

    /// <summary>POST /api/compromisos/{id}/cumplir — marca el compromiso como cumplido.</summary>
    [HttpPost("{id:int}/cumplir")]
    [Authorize(Policy = "actas.crear")]
    public async Task<ActionResult<CompromisoActaDto>> Cumplir(int id) => Ok(await _bot.MarcarCumplidoAsync(id));

    /// <summary>POST /api/compromisos/{id}/vincular-documento — asocia el cambio
    /// documental (ya existente) que cierra este compromiso. "Integrar cambios en
    /// documentos" a partir de una minuta: primero se crea/identifica el Documento
    /// (POST /api/documentos), luego se vincula aquí al compromiso que lo motivó.</summary>
    [HttpPost("{id:int}/vincular-documento")]
    [Authorize(Policy = "actas.crear")]
    public async Task<ActionResult<CompromisoActaDto>> VincularDocumento(int id, VincularDocumentoDto datos) =>
        Ok(await _bot.VincularDocumentoAsync(id, datos.IdDocumento));
}
