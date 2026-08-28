using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>Endpoints del ciclo documental: captura, firma de aprobación, renovación
/// y digitalización (OCR) de documentos físicos escaneados.</summary>
[ApiController]
[Authorize]
[Route("api/documentos")]
public class DocumentosControlador : ControllerBase
{
    private readonly IServicioDocumento _servicioDocumento;
    private readonly IServicioOcr _servicioOcr;
    public DocumentosControlador(IServicioDocumento servicioDocumento, IServicioOcr servicioOcr)
    { _servicioDocumento = servicioDocumento; _servicioOcr = servicioOcr; }

    /// <summary>GET /api/documentos?pagina=1&amp;tamanioPagina=20 — lista documentos
    /// paginados, del más reciente al más antiguo. El tamaño de página se limita
    /// a 100 elementos por consulta.</summary>
    [HttpGet]
    public async Task<ActionResult<PaginaDto<DocumentoDto>>> ObtenerTodos([FromQuery] int pagina = 1, [FromQuery] int tamanioPagina = 20) =>
        Ok(await _servicioDocumento.ObtenerPaginadoAsync(pagina, tamanioPagina));

    /// <summary>GET /api/documentos/resumen — conteos agregados (total, pendientes,
    /// vencidos, aprobados) calculados en la base de datos. Pensado para paneles:
    /// evita que el cliente tenga que traer todos los documentos solo para contarlos.</summary>
    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenDocumentosDto>> ObtenerResumen() => Ok(await _servicioDocumento.ObtenerResumenAsync());

    /// <summary>POST /api/documentos — registra un nuevo documento (queda pendiente de firma).</summary>
    [HttpPost]
    public async Task<ActionResult<DocumentoDto>> Crear(CrearDocumentoDto datos) => Ok(await _servicioDocumento.CrearAsync(datos));

    /// <summary>POST /api/documentos/{id}/firmar — aprueba el documento con el usuario
    /// autenticado. Requiere el permiso fino "documentos.firmar" (RBAC).</summary>
    [HttpPost("{id:int}/firmar")]
    [Authorize(Policy = "documentos.firmar")]
    public async Task<ActionResult<DocumentoDto>> Firmar(int id)
    {
        var idUsuario = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _servicioDocumento.FirmarAsync(id, idUsuario));
    }

    /// <summary>POST /api/documentos/{id}/renovar — crea un nuevo registro pendiente a partir de uno vencido/por vencer.</summary>
    [HttpPost("{id:int}/renovar")]
    public async Task<ActionResult<DocumentoDto>> Renovar(int id) => Ok(await _servicioDocumento.RenovarAsync(id));

    /// <summary>DELETE /api/documentos/{id} — elimina un documento.
    /// Requiere el permiso fino "documentos.eliminar" (RBAC) — acción destructiva.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "documentos.eliminar")]
    public async Task<IActionResult> Eliminar(int id) { await _servicioDocumento.EliminarAsync(id); return NoContent(); }

    /// <summary>POST /api/documentos/{id}/escaneo — sube una foto o imagen escaneada
    /// de un documento físico y ejecuta OCR sobre ella (JPEG, PNG, BMP o TIFF; máx.
    /// 15 MB — un PDF debe convertirse a imagen antes de subirlo). El documento debe
    /// existir de antemano: primero se registra con sus datos estructurados
    /// (POST /api/documentos), luego se adjunta el escaneo como evidencia digital.
    /// Requiere el permiso "documentos.escanear" (RBAC).</summary>
    [HttpPost("{id:int}/escaneo")]
    [Authorize(Policy = "documentos.escanear")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<ActionResult<DigitalizacionDocumentoDto>> Escanear(int id, IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { mensaje = "Debes adjuntar un archivo." });

        await using var flujo = archivo.OpenReadStream();
        var resultado = await _servicioOcr.DigitalizarAsync(id, flujo, archivo.FileName, archivo.ContentType);
        return Ok(resultado);
    }

    /// <summary>GET /api/documentos/{id}/escaneo — consulta la digitalización ya
    /// hecha de un documento (null si nunca se escaneó ninguna evidencia física).</summary>
    [HttpGet("{id:int}/escaneo")]
    public async Task<ActionResult<DigitalizacionDocumentoDto?>> ObtenerEscaneo(int id) =>
        Ok(await _servicioOcr.ObtenerDigitalizacionAsync(id));
}
