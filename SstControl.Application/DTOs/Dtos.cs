namespace SstControl.Aplicacion.DTOs;

/// <summary>Representación de un Documento expuesta por la API (solo lectura).</summary>
public record DocumentoDto(int IdDocumento, string NombreTipo, string NombreColaborador, string Actividad,
    DateOnly FechaCaptura, DateOnly FechaVencimiento, string Estado, string? NombreQuienAprueba);

/// <summary>Datos necesarios para registrar un nuevo Documento.</summary>
public record CrearDocumentoDto(int IdTipoDocumento, string NombreColaborador, string Actividad, DateOnly FechaVencimiento,
    int? IdEmpresa, int? IdSede);

/// <summary>Empresa cliente con sus sedes, tal como se muestra en Administración.</summary>
public record EmpresaDto(int IdEmpresa, string Nombre, List<SedeDto> Sedes);
public record SedeDto(int IdSede, int IdEmpresa, string Nombre);

/// <summary>Representación de un Acta expuesta por la API.</summary>
public record ActaDto(int IdActa, int IdEmpresa, int IdSede, string Tipo, string Titulo, DateOnly Fecha,
    string? Asistentes, string? Notas, string NombreCreador);

/// <summary>Datos necesarios para registrar una nueva Acta (reunión o capacitación).</summary>
public record CrearActaDto(int IdEmpresa, int IdSede, string Tipo, string Titulo, DateOnly Fecha,
    string? Asistentes, string? Notas);

/// <summary>Resultado de un inicio de sesión exitoso: token JWT y los roles/permisos
/// efectivos del usuario (resultado de combinar Usuario → Rol → Perfil → Permiso).</summary>
public record ResultadoAutenticacionDto(string Token, string NombreCompleto, List<string> Roles, List<string> Permisos);
