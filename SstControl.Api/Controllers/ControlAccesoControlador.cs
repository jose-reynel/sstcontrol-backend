using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;

namespace SstControl.Api.Controladores;

/// <summary>
/// Administración del control de acceso: consulta de catálogos (permisos, perfiles,
/// roles, grupos, usuarios) y asignación de roles/grupos a usuarios. Todo el módulo
/// exige el permiso "accesos.administrar" — solo quien lo tenga puede ver o tocar
/// la configuración de quién puede hacer qué en el sistema.
/// </summary>
[ApiController]
[Authorize(Policy = "accesos.administrar")]
[Route("api/control-acceso")]
public class ControlAccesoControlador : ControllerBase
{
    private readonly IServicioControlAcceso _servicio;
    public ControlAccesoControlador(IServicioControlAcceso servicio) => _servicio = servicio;

    /// <summary>GET /api/control-acceso/permisos — catálogo completo de permisos atómicos.</summary>
    [HttpGet("permisos")]
    public async Task<ActionResult<IReadOnlyList<PermisoDto>>> ObtenerPermisos() => Ok(await _servicio.ObtenerPermisosAsync());

    /// <summary>GET /api/control-acceso/perfiles — perfiles (paquetes de permisos) disponibles.</summary>
    [HttpGet("perfiles")]
    public async Task<ActionResult<IReadOnlyList<PerfilDto>>> ObtenerPerfiles() => Ok(await _servicio.ObtenerPerfilesAsync());

    /// <summary>GET /api/control-acceso/roles — roles de negocio y los perfiles que componen cada uno.</summary>
    [HttpGet("roles")]
    public async Task<ActionResult<IReadOnlyList<RolDto>>> ObtenerRoles() => Ok(await _servicio.ObtenerRolesAsync());

    /// <summary>GET /api/control-acceso/grupos — grupos organizativos y sus integrantes.</summary>
    [HttpGet("grupos")]
    public async Task<ActionResult<IReadOnlyList<GrupoDto>>> ObtenerGrupos() => Ok(await _servicio.ObtenerGruposAsync());

    /// <summary>GET /api/control-acceso/usuarios — usuarios con sus roles y grupos asignados.</summary>
    [HttpGet("usuarios")]
    public async Task<ActionResult<IReadOnlyList<UsuarioResumenDto>>> ObtenerUsuarios() => Ok(await _servicio.ObtenerUsuariosAsync());

    /// <summary>POST /api/control-acceso/asignar-rol — agrega un rol a un usuario (no reemplaza los existentes).</summary>
    [HttpPost("asignar-rol")]
    public async Task<IActionResult> AsignarRol(AsignarRolDto datos) { await _servicio.AsignarRolAsync(datos); return NoContent(); }

    /// <summary>POST /api/control-acceso/asignar-grupo — agrega un usuario a un grupo organizativo.</summary>
    [HttpPost("asignar-grupo")]
    public async Task<IActionResult> AsignarGrupo(AsignarGrupoDto datos) { await _servicio.AsignarGrupoAsync(datos); return NoContent(); }
}
