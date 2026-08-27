namespace SstControl.Dominio.Entidades;

// =========================================================================
// CONTROL DE ACCESO (RBAC): Usuario ↔ Grupo ↔ Rol ↔ Perfil ↔ Permiso
// -------------------------------------------------------------------------
// Jerarquía de conceptos, de lo más granular a lo más agregado:
//   Permiso  → acción atómica que el sistema puede autorizar (ej. "documentos.firmar")
//   Perfil   → paquete reutilizable de permisos (ej. "Gestión documental completa")
//   Rol      → nombre de negocio que agrupa uno o varios perfiles (ej. "Administrador SST")
//   Grupo    → agrupación organizativa de usuarios (ej. equipo de una empresa/sede),
//              no otorga permisos por sí sola — es para reportes y alcance organizativo.
//   Usuario  → persona; puede tener varios roles y pertenecer a varios grupos.
// Todas las relaciones Usuario-Rol, Rol-Perfil, Perfil-Permiso y Usuario-Grupo son N:N,
// resueltas con tablas asociativas explícitas (ver más abajo).
// =========================================================================

/// <summary>
/// Acción atómica que el sistema puede autorizar (ej. "documentos.firmar",
/// "empresas.administrar"). Es el nivel más granular del control de acceso —
/// nunca se asigna directamente a un usuario, siempre a través de un Perfil.
/// </summary>
public class Permiso
{
    public int IdPermiso { get; set; }

    /// <summary>Código único usado en el código para verificar autorización (ej. "documentos.firmar").</summary>
    public string Codigo { get; set; } = default!;
    public string Descripcion { get; set; } = default!;

    /// <summary>Módulo al que pertenece (Documentos, Actas, Empresas, Calidad, Capacitación, Admin) — solo agrupa visualmente.</summary>
    public string Modulo { get; set; } = default!;

    public ICollection<PerfilPermiso> Perfiles { get; set; } = new List<PerfilPermiso>();
}

/// <summary>
/// Paquete reutilizable de permisos (plantilla de autorización). Un mismo perfil
/// puede asignarse a varios roles distintos, evitando repetir la misma combinación
/// de permisos una y otra vez.
/// </summary>
public class Perfil
{
    public int IdPerfil { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Descripcion { get; set; }

    public ICollection<PerfilPermiso> Permisos { get; set; } = new List<PerfilPermiso>();
    public ICollection<RolPerfil> Roles { get; set; } = new List<RolPerfil>();
}

/// <summary>Tabla asociativa: qué permisos incluye cada perfil (N:N Perfil↔Permiso).</summary>
public class PerfilPermiso
{
    public int IdPerfil { get; set; }
    public Perfil Perfil { get; set; } = default!;
    public int IdPermiso { get; set; }
    public Permiso Permiso { get; set; } = default!;
}

/// <summary>
/// Rol de negocio (ej. "Administrador SST", "Colaborador de Campo", "Supervisor de Sede").
/// Un rol se compone de uno o varios perfiles — así, crear un rol nuevo es combinar
/// perfiles existentes en vez de definir permisos desde cero.
/// </summary>
public class Rol
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Descripcion { get; set; }

    public ICollection<RolPerfil> Perfiles { get; set; } = new List<RolPerfil>();
    public ICollection<UsuarioRol> Usuarios { get; set; } = new List<UsuarioRol>();
}

/// <summary>Tabla asociativa: qué perfiles componen cada rol (N:N Rol↔Perfil).</summary>
public class RolPerfil
{
    public int IdRol { get; set; }
    public Rol Rol { get; set; } = default!;
    public int IdPerfil { get; set; }
    public Perfil Perfil { get; set; } = default!;
}

/// <summary>
/// Agrupación organizativa de usuarios (ej. "Equipo Constructora Andina"). No otorga
/// permisos — sirve para reportes, alcance organizativo y, opcionalmente, para
/// asociar un conjunto de usuarios a una empresa cliente específica.
/// </summary>
public class Grupo
{
    public int IdGrupo { get; set; }
    public string Nombre { get; set; } = default!;

    /// <summary>Empresa a la que pertenece este grupo, si aplica (opcional).</summary>
    public int? IdEmpresa { get; set; }
    public Empresa? Empresa { get; set; }

    public ICollection<UsuarioGrupo> Usuarios { get; set; } = new List<UsuarioGrupo>();
}

/// <summary>Tabla asociativa: a qué grupos pertenece cada usuario (N:N Usuario↔Grupo).</summary>
public class UsuarioGrupo
{
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = default!;
    public int IdGrupo { get; set; }
    public Grupo Grupo { get; set; } = default!;
}

/// <summary>Tabla asociativa: qué roles tiene cada usuario (N:N Usuario↔Rol) — un usuario
/// puede combinar más de un rol (ej. "Colaborador de Campo" + "Supervisor de Sede").</summary>
public class UsuarioRol
{
    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = default!;
    public int IdRol { get; set; }
    public Rol Rol { get; set; } = default!;
}

/// <summary>
/// Usuario del sistema. Puede ser el profesional SST (administrador) o un colaborador
/// de campo. Sus permisos efectivos resultan de combinar todos los permisos de todos
/// los perfiles de todos sus roles (Usuario → Rol → Perfil → Permiso).
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string NombreUsuario { get; set; } = default!;

    /// <summary>Hash BCrypt de la contraseña — nunca se guarda en texto plano.</summary>
    public string ClaveHash { get; set; } = default!;
    public string NombreCompleto { get; set; } = default!;
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<UsuarioRol> Roles { get; set; } = new List<UsuarioRol>();
    public ICollection<UsuarioGrupo> Grupos { get; set; } = new List<UsuarioGrupo>();

    // ---- Relaciones inversas: todo lo que este usuario ha generado en el sistema ----
    public ICollection<Documento> DocumentosAprobados { get; set; } = new List<Documento>();
    public ICollection<Acta> ActasCreadas { get; set; } = new List<Acta>();
    public ICollection<ProgresoCursoUsuario> ProgresoCursos { get; set; } = new List<ProgresoCursoUsuario>();
    public ICollection<SesionJuego> SesionesJuego { get; set; } = new List<SesionJuego>();
    public ICollection<InsigniaUsuario> Insignias { get; set; } = new List<InsigniaUsuario>();
    public ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = new List<RegistroAuditoria>();
    public ICollection<TokenRenovacion> TokensRenovacion { get; set; } = new List<TokenRenovacion>();
}

/// <summary>
/// Token de larga duración usado exclusivamente para obtener un nuevo JWT sin
/// pedir la contraseña de nuevo (endpoint POST /api/autenticacion/renovar-token).
/// Se guarda con rotación: cada uso emite un token nuevo y revoca el usado
/// (<see cref="Revocado"/> + <see cref="ReemplazadoPor"/>), de forma que un
/// token robado que ya fue usado por el dueño legítimo queda inservible — si
/// alguien intenta reusarlo, se detecta la reutilización.
/// </summary>
public class TokenRenovacion
{
    public int IdTokenRenovacion { get; set; }

    /// <summary>Valor aleatorio opaco (no es un JWT) — se genera con RandomNumberGenerator.</summary>
    public string Token { get; set; } = default!;

    public int IdUsuario { get; set; }
    public Usuario Usuario { get; set; } = default!;

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset FechaExpiracion { get; set; }
    public DateTimeOffset? FechaRevocacion { get; set; }

    /// <summary>Token que lo reemplazó al usarse (null mientras siga vigente).</summary>
    public string? ReemplazadoPor { get; set; }

    public bool Revocado => FechaRevocacion is not null;
    public bool Expirado => DateTimeOffset.UtcNow >= FechaExpiracion;
    public bool Vigente => !Revocado && !Expirado;
}

// =========================================================================
// ORGANIZACIÓN CLIENTE
// =========================================================================

/// <summary>Empresa cliente del profesional SST. Agrupa una o varias sedes.</summary>
public class Empresa
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = default!;
    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Sede> Sedes { get; set; } = new List<Sede>();
    public ICollection<Acta> Actas { get; set; } = new List<Acta>();
    public ICollection<Grupo> Grupos { get; set; } = new List<Grupo>();
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

// =========================================================================
// GESTIÓN DOCUMENTAL
// =========================================================================

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

// =========================================================================
// ACTAS Y REUNIONES (con interoperabilidad Teams / Google Meet / Zoom)
// =========================================================================

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

// =========================================================================
// CAPACITACIÓN Y GAMIFICACIÓN
// =========================================================================

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

// =========================================================================
// CALIDAD Y AUDITORÍA
// =========================================================================

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
