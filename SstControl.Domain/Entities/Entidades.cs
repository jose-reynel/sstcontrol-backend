namespace SstControl.Dominio.Entidades;

/// <summary>
/// Rol de acceso de un usuario dentro del sistema (administrador o colaborador de campo).
/// Determina qué módulos puede ver y qué acciones puede ejecutar cada usuario.
/// </summary>
public class Rol
{
    public int IdRol { get; set; }

    /// <summary>Nombre del rol: "admin" o "colaborador".</summary>
    public string Nombre { get; set; } = default!;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

/// <summary>
/// Usuario del sistema. Puede ser el profesional SST (administrador) o un colaborador
/// de campo que registra documentación y participa en la capacitación.
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }

    /// <summary>Nombre de usuario único usado para iniciar sesión.</summary>
    public string NombreUsuario { get; set; } = default!;

    /// <summary>Hash BCrypt de la contraseña — nunca se guarda en texto plano.</summary>
    public string ClaveHash { get; set; } = default!;

    /// <summary>Nombre completo, usado en la interfaz y en la bitácora de auditoría.</summary>
    public string NombreCompleto { get; set; } = default!;

    public int IdRol { get; set; }
    public Rol Rol { get; set; } = default!;

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    // ---- Relaciones inversas: todo lo que este usuario ha generado en el sistema ----
    public ICollection<Documento> DocumentosAprobados { get; set; } = new List<Documento>();
    public ICollection<Acta> ActasCreadas { get; set; } = new List<Acta>();
    public ICollection<ProgresoCursoUsuario> ProgresoCursos { get; set; } = new List<ProgresoCursoUsuario>();
    public ICollection<SesionJuego> SesionesJuego { get; set; } = new List<SesionJuego>();
    public ICollection<InsigniaUsuario> Insignias { get; set; } = new List<InsigniaUsuario>();
    public ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = new List<RegistroAuditoria>();
}

/// <summary>
/// Empresa cliente del profesional SST. Cada empresa agrupa una o varias sedes
/// donde se realizan visitas, capacitaciones y se gestiona documentación.
/// </summary>
public class Empresa
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = default!;
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Sede> Sedes { get; set; } = new List<Sede>();
    public ICollection<Acta> Actas { get; set; } = new List<Acta>();
}

/// <summary>Sede física de una empresa cliente (ej. una obra, una planta, una bodega).</summary>
public class Sede
{
    public int IdSede { get; set; }
    public int IdEmpresa { get; set; }
    public Empresa Empresa { get; set; } = default!;
    public string Nombre { get; set; } = default!;

    public ICollection<Acta> Actas { get; set; } = new List<Acta>();
}

/// <summary>Catálogo de tipos de documento SST (EPP, alturas, examen médico, etc.).</summary>
public class TipoDocumento
{
    public int IdTipoDocumento { get; set; }
    public string Nombre { get; set; } = default!;
    public ICollection<Documento> Documentos { get; set; } = new List<Documento>();
}

/// <summary>Estado de un documento dentro de su ciclo de vida.</summary>
public enum EstadoDocumento { Pendiente, Aprobado }

/// <summary>
/// Documento SST capturado en campo. Recorre el ciclo: captura → control de tiempo
/// (fecha de vencimiento) → firma de aprobación → vigencia/renovación.
/// </summary>
public class Documento
{
    public int IdDocumento { get; set; }

    public int IdTipoDocumento { get; set; }
    public TipoDocumento TipoDocumento { get; set; } = default!;

    /// <summary>Empresa/sede asociadas (opcionales, para trazabilidad multiempresa).</summary>
    public int? IdEmpresa { get; set; }
    public int? IdSede { get; set; }

    public string NombreColaborador { get; set; } = default!;
    public string Actividad { get; set; } = default!;
    public DateOnly FechaCaptura { get; set; }
    public DateOnly FechaVencimiento { get; set; }

    public EstadoDocumento Estado { get; set; } = EstadoDocumento.Pendiente;

    /// <summary>Usuario que firmó la aprobación — nulo mientras el documento esté pendiente.</summary>
    public int? IdUsuarioAprueba { get; set; }
    public Usuario? UsuarioAprueba { get; set; }
    public DateTimeOffset? FechaFirma { get; set; }
}

/// <summary>Tipo de acta: reunión (ej. COPASST) o capacitación.</summary>
public enum TipoActa { Reunion, Capacitacion }

/// <summary>Plataforma de origen de una reunión sincronizada, o registro manual.</summary>
public enum OrigenReunion { Manual, Teams, GoogleMeet, Zoom }

/// <summary>
/// Acta de una reunión o capacitación realizada en una sede de una empresa cliente.
/// Puede registrarse manualmente (asistente guiado en la app) o sincronizarse
/// automáticamente desde Teams, Google Meet o Zoom.
/// </summary>
public class Acta
{
    public int IdActa { get; set; }

    public int IdEmpresa { get; set; }
    public Empresa Empresa { get; set; } = default!;
    public int IdSede { get; set; }
    public Sede Sede { get; set; } = default!;

    public TipoActa Tipo { get; set; }
    public string Titulo { get; set; } = default!;
    public DateOnly Fecha { get; set; }

    /// <summary>Resumen de asistentes en texto libre — se mantiene por compatibilidad
    /// con el registro manual; la lista detallada vive en AsistentesReunion.</summary>
    public string? Asistentes { get; set; }
    public string? Notas { get; set; }

    public int IdUsuarioCreador { get; set; }
    public Usuario UsuarioCreador { get; set; } = default!;

    // ---- Interoperabilidad con plataformas de reunión externas ----
    public OrigenReunion Origen { get; set; } = OrigenReunion.Manual;
    public string? IdReunionExterna { get; set; }
    public string? UrlIngresoExterna { get; set; }
    public DateTimeOffset? FechaSincronizacion { get; set; }

    public ICollection<AsistenteReunion> AsistentesReunion { get; set; } = new List<AsistenteReunion>();
    public ContenidoReunion? Contenido { get; set; }
}

/// <summary>
/// Registro de un asistente real a una reunión, con su hora de entrada/salida —
/// trazabilidad detallada que reemplaza al simple texto libre cuando la reunión
/// se sincroniza desde Teams/Meet/Zoom.
/// </summary>
public class AsistenteReunion
{
    public int IdAsistente { get; set; }
    public int IdActa { get; set; }
    public Acta Acta { get; set; } = default!;

    public string Nombre { get; set; } = default!;
    public string? CorreoElectronico { get; set; }
    public DateTimeOffset? HoraIngreso { get; set; }
    public DateTimeOffset? HoraSalida { get; set; }
    public int? DuracionMinutos { get; set; }
}

/// <summary>Contenido adicional de una reunión traído desde la plataforma de origen
/// (resumen/transcripción y/o enlace de grabación). Relación 1 a 1 con Acta.</summary>
public class ContenidoReunion
{
    public int IdActa { get; set; } // clave primaria y foránea a la vez
    public Acta Acta { get; set; } = default!;

    public string? Resumen { get; set; }
    public string? UrlGrabacion { get; set; }

    /// <summary>"transcript" | "recording" | "summary".</summary>
    public string TipoContenido { get; set; } = "summary";
    public DateTimeOffset FechaObtencion { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Curso corto de capacitación SST, compuesto de lecciones y un examen.</summary>
public class Curso
{
    public int IdCurso { get; set; }
    public string Titulo { get; set; } = default!;
    public string Icono { get; set; } = default!;
    public string Resumen { get; set; } = default!;

    public ICollection<Leccion> Lecciones { get; set; } = new List<Leccion>();
    public ICollection<PreguntaQuiz> Preguntas { get; set; } = new List<PreguntaQuiz>();
    public ICollection<ProgresoCursoUsuario> Progreso { get; set; } = new List<ProgresoCursoUsuario>();
}

/// <summary>Lección individual dentro de un curso, mostrada en orden (Orden).</summary>
public class Leccion
{
    public int IdLeccion { get; set; }
    public int IdCurso { get; set; }
    public Curso Curso { get; set; } = default!;
    public string Titulo { get; set; } = default!;
    public string ContenidoHtml { get; set; } = default!;
    public int Orden { get; set; }
}

/// <summary>Pregunta de opción múltiple del examen de un curso.</summary>
public class PreguntaQuiz
{
    public int IdPregunta { get; set; }
    public int IdCurso { get; set; }
    public Curso Curso { get; set; } = default!;
    public string Texto { get; set; } = default!;
    public ICollection<OpcionQuiz> Opciones { get; set; } = new List<OpcionQuiz>();
}

/// <summary>Opción de respuesta de una pregunta — exactamente una debe ser correcta.</summary>
public class OpcionQuiz
{
    public int IdOpcion { get; set; }
    public int IdPregunta { get; set; }
    public PreguntaQuiz Pregunta { get; set; } = default!;
    public string Texto { get; set; } = default!;
    public bool EsCorrecta { get; set; }
}

/// <summary>
/// Resultado de un usuario en el examen de un curso. Aprueba con 70% o más.
/// Único registro por combinación usuario+curso (se sobreescribe al reintentar).
/// </summary>
public class ProgresoCursoUsuario
{
    public int IdProgreso { get; set; }
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = default!;
    public int IdCurso { get; set; }
    public Curso Curso { get; set; } = default!;

    public int Puntaje { get; set; }
    public bool Aprobado { get; set; }
    public DateTimeOffset FechaFinalizacion { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Partida del juego "Reto rápido SST" (trivia contrarreloj) jugada por un usuario.</summary>
public class SesionJuego
{
    public int IdSesionJuego { get; set; }
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = default!;

    public int CantidadCorrectas { get; set; }
    public int TotalPreguntas { get; set; }
    public int Puntos { get; set; }
    public int MejorRacha { get; set; }
    public DateTimeOffset FechaJuego { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Insignia/logro que un usuario puede desbloquear (ej. "Experto SST").</summary>
public class Insignia
{
    public int IdInsignia { get; set; }

    /// <summary>Código interno usado por la lógica de desbloqueo (ej. "b_first").</summary>
    public string Codigo { get; set; } = default!;
    public string Etiqueta { get; set; } = default!;
    public string Icono { get; set; } = default!;

    public ICollection<InsigniaUsuario> UsuariosConInsignia { get; set; } = new List<InsigniaUsuario>();
}

/// <summary>Relación N a N entre Usuario e Insignia: qué insignia obtuvo cada usuario y cuándo.</summary>
public class InsigniaUsuario
{
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = default!;
    public int IdInsignia { get; set; }
    public Insignia Insignia { get; set; } = default!;
    public DateTimeOffset FechaObtencion { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Ítem de la lista de autoevaluación (checklist) de auditoría interna de calidad.</summary>
public class ItemChecklist
{
    public int IdItemChecklist { get; set; }
    public string Etiqueta { get; set; } = default!;
    public RespuestaChecklist? Respuesta { get; set; }
}

/// <summary>Estado actual (marcado/no marcado) de un ítem del checklist — relación 1 a 1.</summary>
public class RespuestaChecklist
{
    public int IdItemChecklist { get; set; } // clave primaria y foránea a la vez
    public ItemChecklist ItemChecklist { get; set; } = default!;

    public bool Marcado { get; set; }
    public int IdUsuarioActualiza { get; set; }
    public Usuario UsuarioActualiza { get; set; } = default!;
    public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Registro cronológico de acciones relevantes del sistema (bitácora de auditoría):
/// quién hizo qué y cuándo, en todos los módulos.
/// </summary>
public class RegistroAuditoria
{
    public long IdRegistroAuditoria { get; set; }

    /// <summary>Usuario que ejecutó la acción — nulo para eventos automáticos del sistema.</summary>
    public int? IdUsuario { get; set; }
    public Usuario? Usuario { get; set; }

    public string Accion { get; set; } = default!;
    public string? Detalle { get; set; }
    public DateTimeOffset FechaHora { get; set; } = DateTimeOffset.UtcNow;
}
