namespace SstControl.Aplicacion.DTOs;

// ---- Control de acceso (RBAC): DTOs de solo lectura para administración ----

public record PermisoDto(int IdPermiso, string Codigo, string Descripcion, string Modulo);
public record PerfilDto(int IdPerfil, string Nombre, string? Descripcion, List<string> CodigosPermiso);
public record RolDto(int IdRol, string Nombre, string? Descripcion, List<string> NombresPerfiles);
public record GrupoDto(int IdGrupo, string Nombre, int? IdEmpresa, List<string> UsuariosDelGrupo);
public record UsuarioResumenDto(int IdUsuario, string NombreUsuario, string NombreCompleto, List<string> Roles, List<string> Grupos);

public record AsignarRolDto(int IdUsuario, int IdRol);
public record AsignarGrupoDto(int IdUsuario, int IdGrupo);
