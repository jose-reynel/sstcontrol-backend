using System.ComponentModel.DataAnnotations;

namespace SstControl.Aplicacion.DTOs;

/// <summary>Representación de un Documento expuesta por la API (solo lectura).</summary>
public record DocumentoDto(int IdDocumento, string NombreTipo, string NombreColaborador, string Actividad,
    DateOnly FechaCaptura, DateOnly FechaVencimiento, string Estado, string? NombreQuienAprueba);

/// <summary>Datos necesarios para registrar un nuevo Documento. Las anotaciones se
/// validan automáticamente por el filtro implícito de [ApiController] — una petición
/// inválida nunca llega al servicio de negocio; la API responde 400 con
/// ValidationProblemDetails (RFC 7807) antes de tocar la base de datos.</summary>
public record CrearDocumentoDto(
    [property: Range(1, int.MaxValue, ErrorMessage = "El tipo de documento es obligatorio.")]
    int IdTipoDocumento,
    [property: Required(AllowEmptyStrings = false), MaxLength(200)]
    string NombreColaborador,
    [property: Required(AllowEmptyStrings = false), MaxLength(300)]
    string Actividad,
    DateOnly FechaVencimiento,
    [property: Range(1, int.MaxValue)] int? IdEmpresa,
    [property: Range(1, int.MaxValue)] int? IdSede);

/// <summary>Empresa cliente con sus sedes, tal como se muestra en Administración.</summary>
public record EmpresaDto(int IdEmpresa, string Nombre, List<SedeDto> Sedes);
public record SedeDto(int IdSede, int IdEmpresa, string Nombre);

/// <summary>Representación de un Acta expuesta por la API.</summary>
public record ActaDto(int IdActa, int IdEmpresa, int IdSede, string Tipo, string Titulo, DateOnly Fecha,
    string? Asistentes, string? Notas, string NombreCreador);

/// <summary>Datos necesarios para registrar una nueva Acta (reunión o capacitación).</summary>
public record CrearActaDto(
    [property: Range(1, int.MaxValue)] int IdEmpresa,
    [property: Range(1, int.MaxValue)] int IdSede,
    [property: Required, RegularExpression("^(Reunion|Capacitacion)$", ErrorMessage = "Tipo debe ser 'Reunion' o 'Capacitacion'.")]
    string Tipo,
    [property: Required(AllowEmptyStrings = false), MaxLength(200)]
    string Titulo,
    DateOnly Fecha,
    [property: MaxLength(2000)] string? Asistentes,
    [property: MaxLength(4000)] string? Notas);

/// <summary>Resultado de un inicio de sesión (o renovación) exitoso: el JWT de
/// corta duración para llamar a la API, el token de renovación de larga duración
/// (opaco, no es un JWT) para obtener un JWT nuevo sin pedir la contraseña de
/// nuevo, y los roles/permisos efectivos del usuario.</summary>
public record ResultadoAutenticacionDto(string Token, string TokenRenovacion, string NombreCompleto, List<string> Roles, List<string> Permisos);

/// <summary>
/// Envoltorio genérico de paginación para listas que pueden crecer sin límite
/// (documentos, actas). Evita traer miles de filas en una sola respuesta —
/// requisito básico de escalabilidad para cualquier listado de una app real.
/// </summary>
public record PaginaDto<T>(IReadOnlyList<T> Elementos, int Pagina, int TamanioPagina, int TotalElementos)
{
    public int TotalPaginas => TamanioPagina <= 0 ? 0 : (int)Math.Ceiling(TotalElementos / (double)TamanioPagina);
}
