using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>Administración del modelo RBAC: consulta el catálogo de permisos/perfiles/
/// roles/grupos, y gestiona las asignaciones de rol y grupo a cada usuario.</summary>
public class ServicioControlAcceso : IServicioControlAcceso
{
    private readonly ContextoBaseDatos _contexto;
    public ServicioControlAcceso(ContextoBaseDatos contexto) => _contexto = contexto;

    public async Task<IReadOnlyList<PermisoDto>> ObtenerPermisosAsync() =>
        await _contexto.Permisos
            .Select(p => new PermisoDto(p.IdPermiso, p.Codigo, p.Descripcion, p.Modulo))
            .ToListAsync();

    public async Task<IReadOnlyList<PerfilDto>> ObtenerPerfilesAsync() =>
        await _contexto.Perfiles.Include(p => p.Permisos).ThenInclude(pp => pp.Permiso)
            .Select(p => new PerfilDto(p.IdPerfil, p.Nombre, p.Descripcion,
                p.Permisos.Select(pp => pp.Permiso.Codigo).ToList()))
            .ToListAsync();

    public async Task<IReadOnlyList<RolDto>> ObtenerRolesAsync() =>
        await _contexto.Roles.Include(r => r.Perfiles).ThenInclude(rp => rp.Perfil)
            .Select(r => new RolDto(r.IdRol, r.Nombre, r.Descripcion,
                r.Perfiles.Select(rp => rp.Perfil.Nombre).ToList()))
            .ToListAsync();

    public async Task<IReadOnlyList<GrupoDto>> ObtenerGruposAsync() =>
        await _contexto.Grupos.Include(g => g.Usuarios).ThenInclude(ug => ug.Usuario)
            .Select(g => new GrupoDto(g.IdGrupo, g.Nombre, g.IdEmpresa,
                g.Usuarios.Select(ug => ug.Usuario.NombreCompleto).ToList()))
            .ToListAsync();

    public async Task<IReadOnlyList<UsuarioResumenDto>> ObtenerUsuariosAsync() =>
        await _contexto.Usuarios
            .Include(u => u.Roles).ThenInclude(ur => ur.Rol)
            .Include(u => u.Grupos).ThenInclude(ug => ug.Grupo)
            .Select(u => new UsuarioResumenDto(u.IdUsuario, u.NombreUsuario, u.NombreCompleto,
                u.Roles.Select(ur => ur.Rol.Nombre).ToList(),
                u.Grupos.Select(ug => ug.Grupo.Nombre).ToList()))
            .ToListAsync();

    public async Task AsignarRolAsync(AsignarRolDto datos)
    {
        var yaAsignado = await _contexto.UsuarioRoles.AnyAsync(ur => ur.IdUsuario == datos.IdUsuario && ur.IdRol == datos.IdRol);
        if (yaAsignado) return; // idempotente: no duplica la asignación
        _contexto.UsuarioRoles.Add(new UsuarioRol { IdUsuario = datos.IdUsuario, IdRol = datos.IdRol });
        await _contexto.SaveChangesAsync();
    }

    public async Task AsignarGrupoAsync(AsignarGrupoDto datos)
    {
        var yaAsignado = await _contexto.UsuarioGrupos.AnyAsync(ug => ug.IdUsuario == datos.IdUsuario && ug.IdGrupo == datos.IdGrupo);
        if (yaAsignado) return;
        _contexto.UsuarioGrupos.Add(new UsuarioGrupo { IdUsuario = datos.IdUsuario, IdGrupo = datos.IdGrupo });
        await _contexto.SaveChangesAsync();
    }
}
