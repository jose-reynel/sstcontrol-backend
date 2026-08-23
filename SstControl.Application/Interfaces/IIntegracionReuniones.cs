namespace SstControl.Aplicacion.Integraciones;

/// <summary>Plataformas de reunión soportadas. El valor debe coincidir con OrigenReunion en Dominio.</summary>
public enum ProveedorReunion { Teams, GoogleMeet, Zoom }

/// <summary>Datos generales de una reunión traídos desde la plataforma externa.</summary>
public record ReunionExternaDto(string IdReunionExterna, string Titulo, DateTimeOffset FechaInicio,
    DateTimeOffset? FechaFin, string? UrlIngreso);

/// <summary>Un asistente real de la reunión, con su hora de entrada/salida si la plataforma la reporta.</summary>
public record AsistenteExternoDto(string Nombre, string? CorreoElectronico, DateTimeOffset? HoraIngreso, DateTimeOffset? HoraSalida);

/// <summary>Contenido adicional de la reunión (resumen/transcripción o grabación), si existe.</summary>
public record ContenidoExternoDto(string? Resumen, string? UrlGrabacion, string TipoContenido);

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
