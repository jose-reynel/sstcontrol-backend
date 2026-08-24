using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>Endpoints del ciclo documental: captura, firma de aprobación y renovación.</summary>
[ApiController]
[Authorize]
[Route("api/documentos")]
public class DocumentosControlador : ControllerBase
{
    private readonly IServicioDocumento _servicioDocumento;
    public DocumentosControlador(IServicioDocumento servicioDocumento) => _servicioDocumento = servicioDocumento;

    /// <summary>GET /api/documentos — lista todos los documentos.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentoDto>>> ObtenerTodos() => Ok(await _servicioDocumento.ObtenerTodosAsync());

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
}
