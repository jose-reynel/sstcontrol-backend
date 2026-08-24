using SstControl.Aplicacion.DTOs;

namespace SstControl.Aplicacion.Interfaces;

/// <summary>Consulta y administración del modelo de control de acceso:
/// permisos, perfiles, roles, grupos, y sus asignaciones a usuarios.</summary>
public interface IServicioControlAcceso
{
    Task<IReadOnlyList<PermisoDto>> ObtenerPermisosAsync();
    Task<IReadOnlyList<PerfilDto>> ObtenerPerfilesAsync();
    Task<IReadOnlyList<RolDto>> ObtenerRolesAsync();
    Task<IReadOnlyList<GrupoDto>> ObtenerGruposAsync();
    Task<IReadOnlyList<UsuarioResumenDto>> ObtenerUsuariosAsync();

    /// <summary>Asigna un rol adicional a un usuario (no reemplaza los que ya tiene).</summary>
    Task AsignarRolAsync(AsignarRolDto datos);

    /// <summary>Agrega un usuario a un grupo organizativo.</summary>
    Task AsignarGrupoAsync(AsignarGrupoDto datos);
}
