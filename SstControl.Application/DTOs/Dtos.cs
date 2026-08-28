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

// ---- Bot de minutas: seguimiento de compromisos surgidos de una Acta ----

/// <summary>Un compromiso/acuerdo de seguimiento de una Acta, ya sea extraído por el
/// bot de minutas o agregado a mano.</summary>
public record CompromisoActaDto(int IdCompromiso, int IdActa, string Descripcion, string? Responsable,
    DateOnly? FechaLimite, string Estado, string Origen, int? IdDocumentoRelacionado, string? NombreDocumentoRelacionado);

/// <summary>Datos para agregar un compromiso a mano (el bot usa su propio flujo interno).</summary>
public record CrearCompromisoDto(
    [property: Required(AllowEmptyStrings = false), MaxLength(500)] string Descripcion,
    [property: MaxLength(150)] string? Responsable,
    DateOnly? FechaLimite,
    [property: Range(1, int.MaxValue)] int? IdDocumentoRelacionado);

/// <summary>Vincula un compromiso ya existente al documento cuyo cambio lo cierra.</summary>
public record VincularDocumentoDto([property: Range(1, int.MaxValue)] int IdDocumento);

/// <summary>Resultado de una pasada del bot de minutas sobre el contenido de una
/// reunión: el resumen que usó como fuente y los compromisos que extrajo.</summary>
public record MinutaGeneradaDto(string? TextoFuente, List<CompromisoActaDto> Compromisos);

// ---- Digitalización de documentos físicos (OCR) ----

/// <summary>Resultado de escanear un documento físico — el insumo digital y
/// buscable que queda asociado al Documento.</summary>
public record DigitalizacionDocumentoDto(int IdDocumento, string NombreArchivoOriginal, string TipoContenido,
    long TamanioBytes, string? TextoExtraido, double? Confianza, DateTimeOffset FechaEscaneo);

/// <summary>Resultado de un inicio de sesión (o renovación) exitoso: el JWT de
/// corta duración para llamar a la API, el token de renovación de larga duración
/// (opaco, no es un JWT) para obtener un JWT nuevo sin pedir la contraseña de
/// nuevo, y los roles/permisos efectivos del usuario.</summary>
public record ResultadoAutenticacionDto(string Token, string TokenRenovacion, string NombreCompleto, List<string> Roles, List<string> Permisos);

/// <summary>Conteos agregados del ciclo documental, calculados en la base de datos
/// (no en el cliente) — así el Panel no necesita traer todos los documentos para
/// mostrar cifras correctas, sin importar cuántos miles haya.</summary>
public record ResumenDocumentosDto(int Total, int Pendientes, int Vencidos, int Aprobados);

/// <summary>
/// Envoltorio genérico de paginación para listas que pueden crecer sin límite
/// (documentos, actas). Evita traer miles de filas en una sola respuesta —
/// requisito básico de escalabilidad para cualquier listado de una app real.
/// </summary>
public record PaginaDto<T>(IReadOnlyList<T> Elementos, int Pagina, int TamanioPagina, int TotalElementos)
{
    public int TotalPaginas => TamanioPagina <= 0 ? 0 : (int)Math.Ceiling(TotalElementos / (double)TamanioPagina);
}
