using SstControl.Aplicacion.DTOs;

namespace SstControl.Aplicacion.Integraciones;

/// <summary>Plataformas de reunión soportadas. El valor debe coincidir con OrigenReunion en Dominio.</summary>
public enum ProveedorReunion { Teams, GoogleMeet, Zoom, Webex }

/// <summary>Datos generales de una reunión traídos desde la plataforma externa.</summary>
public record ReunionExternaDto(string IdReunionExterna, string Titulo, DateTimeOffset FechaInicio,
    DateTimeOffset? FechaFin, string? UrlIngreso);

/// <summary>Un asistente real de la reunión, con su hora de entrada/salida si la plataforma la reporta.</summary>
public record AsistenteExternoDto(string Nombre, string? CorreoElectronico, DateTimeOffset? HoraIngreso, DateTimeOffset? HoraSalida);

/// <summary>Contenido adicional de la reunión (resumen/transcripción o grabación), si existe.</summary>
public record ContenidoExternoDto(string? Resumen, string? UrlGrabacion, string TipoContenido);

// ---- Bot de minutas: interpretación del contenido de una reunión ----

/// <summary>Compromiso/acuerdo candidato detectado en el texto de una reunión.</summary>
public record CompromisoExtraidoDto(string Descripcion, string? Responsable, DateOnly? FechaLimite);

/// <summary>Resultado de interpretar el contenido textual de una reunión: un
/// extracto/vista previa del texto fuente y los compromisos candidatos detectados.</summary>
public record MinutaExtraidaDto(string? Resumen, IReadOnlyList<CompromisoExtraidoDto> Compromisos);

/// <summary>
/// Puerto de extracción de minutas: dado el texto fuente de una reunión (la
/// transcripción o el resumen que el conector correspondiente ya dejó en
/// ContenidoReunion), produce un extracto y una lista de compromisos candidatos.
/// La implementación por defecto (ver ServicioResumenReunionHeuristico en
/// Infrastructure) usa reglas/patrones de texto — NO es un modelo de lenguaje, no
/// "entiende" la reunión. Es, a propósito, el punto de extensión para poder
/// enchufar más adelante un proveedor de IA real (ej. la API de Anthropic) sin
/// tocar nada de quien consume esta interfaz — solo cambiaría el registro en
/// Program.cs.
/// </summary>
public interface IServicioResumenReunion
{
    MinutaExtraidaDto ExtraerMinuta(string textoFuente);
}

/// <summary>
/// Puerto común de integración: cada plataforma (Teams/Meet/Zoom) implementa esta
/// interfaz con su propio conector a nivel developer (OAuth + endpoints REST propios).
/// </summary>
public interface IConectorReunion
{
    ProveedorReunion Proveedor { get; }
    Task<ReunionExternaDto> ObtenerReunionAsync(string idReunionExterna);
    Task<IReadOnlyList<AsistenteExternoDto>> ObtenerAsistenciaAsync(string idReunionExterna);
    Task<ContenidoExternoDto?> ObtenerContenidoAsync(string idReunionExterna);
}

/// <summary>Resuelve el conector correcto según la plataforma solicitada.</summary>
public interface IFabricaConectoresReunion
{
    IConectorReunion Resolver(ProveedorReunion proveedor);
}

/// <summary>
/// Orquesta la sincronización: llama al conector correspondiente y persiste
/// Acta + AsistentesReunion + ContenidoReunion en el sistema de información central.
/// </summary>
public interface IServicioSincronizacionReuniones
{
    Task<int> SincronizarReunionAsync(ProveedorReunion proveedor, string idReunionExterna, int idEmpresa, int idSede,
        SstControl.Dominio.Entidades.TipoActa tipo, int idUsuarioCreador);
}

/// <summary>
/// Resuelve a qué empresa/sede pertenece una reunión externa, a partir del
/// token de correlación que cada plataforma reenvía en su webhook — ver
/// MapeoOrigenReunion (Dominio) para el mecanismo real de cada una. También
/// administra el catálogo de mapeos (alta y consulta).
/// </summary>
public interface IServicioMapeoReunion
{
    /// <summary>Null si no hay ningún mapeo configurado para ese token — el
    /// webhook debe descartar el evento sin fallar (no hay a qué cliente
    /// asociar la reunión).</summary>
    Task<MapeoOrigenReunionDto?> ResolverAsync(ProveedorReunion proveedor, string tokenCorrelacion);

    Task<MapeoOrigenReunionDto> CrearAsync(CrearMapeoOrigenReunionDto datos);
    Task<IReadOnlyList<MapeoOrigenReunionDto>> ObtenerTodosAsync();
}
