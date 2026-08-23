using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>Gestión de empresas clientes y sus sedes (solo administrador puede crear).</summary>
[ApiController]
[Authorize]
[Route("api/empresas")]
public class EmpresasControlador : ControllerBase
{
    private readonly IServicioEmpresa _servicioEmpresa;
    public EmpresasControlador(IServicioEmpresa servicioEmpresa) => _servicioEmpresa = servicioEmpresa;

    /// <summary>GET /api/empresas — lista empresas con sus sedes anidadas.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmpresaDto>>> ObtenerTodas() => Ok(await _servicioEmpresa.ObtenerTodasConSedesAsync());

    /// <summary>POST /api/empresas — crea una empresa cliente nueva.</summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmpresaDto>> Crear([FromBody] string nombre) => Ok(await _servicioEmpresa.CrearEmpresaAsync(nombre));

    /// <summary>POST /api/empresas/{idEmpresa}/sedes — agrega una sede a la empresa.</summary>
    [HttpPost("{idEmpresa:int}/sedes")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<SedeDto>> CrearSede(int idEmpresa, [FromBody] string nombre) =>
        Ok(await _servicioEmpresa.CrearSedeAsync(idEmpresa, nombre));
}

/// <summary>Gestión de actas de reuniones y capacitaciones registradas manualmente.</summary>
[ApiController]
[Authorize]
[Route("api/actas")]
public class ActasControlador : ControllerBase
{
    private readonly IServicioActa _servicioActa;
    public ActasControlador(IServicioActa servicioActa) => _servicioActa = servicioActa;

    /// <summary>GET /api/actas — lista todas las actas, más recientes primero.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ActaDto>>> ObtenerTodas() => Ok(await _servicioActa.ObtenerTodasAsync());

    /// <summary>POST /api/actas — registra una nueva acta (reunión o capacitación).</summary>
    [HttpPost]
    public async Task<ActionResult<ActaDto>> Crear(CrearActaDto datos)
    {
        var idUsuario = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _servicioActa.CrearAsync(datos, idUsuario));
    }
}
