using Microsoft.EntityFrameworkCore;
using SstControl.Dominio.Entidades;

namespace SstControl.Infraestructura.Persistencia;

/// <summary>
/// Contexto de base de datos (EF Core sobre PostgreSQL). Aquí se configura, con
/// Fluent API, cada relación y cardinalidad definida en el Modelo Entidad-Relación
/// (ver mer-sst.mermaid / mer-sst.png) — claves foráneas, comportamiento ante
/// borrado, índices únicos y las tablas asociativas del modelo de control de acceso.
/// </summary>
public class ContextoBaseDatos : DbContext
{
    public ContextoBaseDatos(DbContextOptions<ContextoBaseDatos> opciones) : base(opciones) { }

    // ---- Control de acceso (RBAC) ----
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<PerfilPermiso> PerfilPermisos => Set<PerfilPermiso>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<RolPerfil> RolPerfiles => Set<RolPerfil>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<UsuarioGrupo> UsuarioGrupos => Set<UsuarioGrupo>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenRenovacion> TokensRenovacion => Set<TokenRenovacion>();

    // ---- Organización cliente ----
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sede> Sedes => Set<Sede>();

    // ---- Gestión documental ----
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<DigitalizacionDocumento> DigitalizacionesDocumento => Set<DigitalizacionDocumento>();

    // ---- Actas y reuniones ----
    public DbSet<Acta> Actas => Set<Acta>();
    public DbSet<AsistenteReunion> AsistentesReunion => Set<AsistenteReunion>();
    public DbSet<ContenidoReunion> ContenidosReunion => Set<ContenidoReunion>();
    public DbSet<CompromisoActa> CompromisosActa => Set<CompromisoActa>();

    // ---- Capacitación y gamificación ----
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Leccion> Lecciones => Set<Leccion>();
    public DbSet<PreguntaQuiz> PreguntasQuiz => Set<PreguntaQuiz>();
    public DbSet<OpcionQuiz> OpcionesQuiz => Set<OpcionQuiz>();
    public DbSet<ProgresoCursoUsuario> ProgresoCursoUsuario => Set<ProgresoCursoUsuario>();
    public DbSet<SesionJuego> SesionesJuego => Set<SesionJuego>();
    public DbSet<Insignia> Insignias => Set<Insignia>();
    public DbSet<InsigniaUsuario> InsigniasUsuario => Set<InsigniaUsuario>();

    // ---- Calidad y auditoría ----
    public DbSet<ItemChecklist> ItemsChecklist => Set<ItemChecklist>();
    public DbSet<RespuestaChecklist> RespuestasChecklist => Set<RespuestaChecklist>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        ConfigurarControlDeAcceso(modelo);
        ConfigurarOrganizacionCliente(modelo);
        ConfigurarGestionDocumental(modelo);
        ConfigurarActasYReuniones(modelo);
        ConfigurarCapacitacionYGamificacion(modelo);
        ConfigurarCalidadYAuditoria(modelo);
    }

    /// <summary>
    /// Configura el núcleo de control de acceso: Permiso ← Perfil ← Rol ← Usuario,
    /// más Grupo como agrupación organizativa. Todas las relaciones intermedias son
    /// N:N, cada una resuelta con su propia tabla asociativa de clave compuesta.
    /// </summary>
    private static void ConfigurarControlDeAcceso(ModelBuilder modelo)
    {
        // Las claves primarias de una sola columna usan el prefijo "Id" + nombre
        // (ej. "IdPermiso") en vez del sufijo que reconoce la convención por
        // defecto de EF Core ("Id" o "<Entidad>Id") — sin esto, EF Core no
        // identificaría la clave primaria y el modelo no llegaría a construirse.
        modelo.Entity<Permiso>().HasKey(p => p.IdPermiso);
        modelo.Entity<Perfil>().HasKey(p => p.IdPerfil);
        modelo.Entity<Rol>().HasKey(r => r.IdRol);
        modelo.Entity<Grupo>().HasKey(g => g.IdGrupo);
        modelo.Entity<Usuario>().HasKey(u => u.IdUsuario);
        modelo.Entity<TokenRenovacion>().HasKey(t => t.IdTokenRenovacion);

        modelo.Entity<Permiso>().HasIndex(p => p.Codigo).IsUnique();
        modelo.Entity<Perfil>().HasIndex(p => p.Nombre).IsUnique();
        modelo.Entity<Rol>().HasIndex(r => r.Nombre).IsUnique();

        // PERFIL (N) --- (N) PERMISO, vía PERFIL_PERMISO
        modelo.Entity<PerfilPermiso>().HasKey(pp => new { pp.IdPerfil, pp.IdPermiso });
        modelo.Entity<PerfilPermiso>()
            .HasOne(pp => pp.Perfil).WithMany(p => p.Permisos)
            .HasForeignKey(pp => pp.IdPerfil).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<PerfilPermiso>()
            .HasOne(pp => pp.Permiso).WithMany(p => p.Perfiles)
            .HasForeignKey(pp => pp.IdPermiso).OnDelete(DeleteBehavior.Cascade);

        // ROL (N) --- (N) PERFIL, vía ROL_PERFIL
        modelo.Entity<RolPerfil>().HasKey(rp => new { rp.IdRol, rp.IdPerfil });
        modelo.Entity<RolPerfil>()
            .HasOne(rp => rp.Rol).WithMany(r => r.Perfiles)
            .HasForeignKey(rp => rp.IdRol).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<RolPerfil>()
            .HasOne(rp => rp.Perfil).WithMany(p => p.Roles)
            .HasForeignKey(rp => rp.IdPerfil).OnDelete(DeleteBehavior.Cascade);

        // USUARIO (N) --- (N) ROL, vía USUARIO_ROL — un usuario puede combinar varios roles
        modelo.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();
        modelo.Entity<UsuarioRol>().HasKey(ur => new { ur.IdUsuario, ur.IdRol });
        modelo.Entity<UsuarioRol>()
            .HasOne(ur => ur.Usuario).WithMany(u => u.Roles)
            .HasForeignKey(ur => ur.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<UsuarioRol>()
            .HasOne(ur => ur.Rol).WithMany(r => r.Usuarios)
            .HasForeignKey(ur => ur.IdRol).OnDelete(DeleteBehavior.Cascade);

        // USUARIO (N) --- (N) GRUPO, vía USUARIO_GRUPO
        modelo.Entity<UsuarioGrupo>().HasKey(ug => new { ug.IdUsuario, ug.IdGrupo });
        modelo.Entity<UsuarioGrupo>()
            .HasOne(ug => ug.Usuario).WithMany(u => u.Grupos)
            .HasForeignKey(ug => ug.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<UsuarioGrupo>()
            .HasOne(ug => ug.Grupo).WithMany(g => g.Usuarios)
            .HasForeignKey(ug => ug.IdGrupo).OnDelete(DeleteBehavior.Cascade);

        // GRUPO (N) --- (1) EMPRESA [opcional] — un grupo puede representar al equipo de una empresa
        modelo.Entity<Grupo>()
            .HasOne(g => g.Empresa).WithMany(e => e.Grupos)
            .HasForeignKey(g => g.IdEmpresa).OnDelete(DeleteBehavior.SetNull).IsRequired(false);

        // USUARIO (1) --- (N) TOKEN_RENOVACION — historial de tokens de renovación
        // (rotados en cada uso); se conserva para poder detectar reutilización de
        // un token ya rotado (indicio de robo) en vez de borrarlo al usarse.
        modelo.Entity<TokenRenovacion>().HasIndex(t => t.Token).IsUnique();
        modelo.Entity<TokenRenovacion>()
            .HasOne(t => t.Usuario).WithMany(u => u.TokensRenovacion)
            .HasForeignKey(t => t.IdUsuario).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarOrganizacionCliente(ModelBuilder modelo)
    {
        modelo.Entity<Empresa>().HasKey(e => e.IdEmpresa);
        modelo.Entity<Sede>().HasKey(s => s.IdSede);

        // EMPRESA (1) --- (N) SEDE
        modelo.Entity<Sede>()
            .HasOne(s => s.Empresa).WithMany(e => e.Sedes)
            .HasForeignKey(s => s.IdEmpresa).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarGestionDocumental(ModelBuilder modelo)
    {
        modelo.Entity<TipoDocumento>().HasKey(t => t.IdTipoDocumento);
        modelo.Entity<Documento>().HasKey(d => d.IdDocumento);

        // TIPO_DOCUMENTO (1) --- (N) DOCUMENTO
        modelo.Entity<Documento>()
            .HasOne(d => d.TipoDocumento).WithMany(t => t.Documentos)
            .HasForeignKey(d => d.IdTipoDocumento).OnDelete(DeleteBehavior.Restrict);
        // USUARIO (1) --- (N) DOCUMENTO [aprueba, opcional 0..N]
        modelo.Entity<Documento>()
            .HasOne(d => d.UsuarioAprueba).WithMany(u => u.DocumentosAprobados)
            .HasForeignKey(d => d.IdUsuarioAprueba).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        modelo.Entity<Documento>().Property(d => d.Estado).HasConversion<string>().HasMaxLength(20);

        // DOCUMENTO (1) --- (1) DIGITALIZACION_DOCUMENTO [opcional] — insumo OCR de un
        // documento físico escaneado; nulo si el documento nació digital.
        modelo.Entity<DigitalizacionDocumento>().HasKey(d => d.IdDocumento);
        modelo.Entity<DigitalizacionDocumento>()
            .HasOne(d => d.Documento).WithOne(doc => doc.Digitalizacion)
            .HasForeignKey<DigitalizacionDocumento>(d => d.IdDocumento).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarActasYReuniones(ModelBuilder modelo)
    {
        modelo.Entity<Acta>().HasKey(a => a.IdActa);
        modelo.Entity<AsistenteReunion>().HasKey(a => a.IdAsistente);
        modelo.Entity<CompromisoActa>().HasKey(c => c.IdCompromiso);

        // EMPRESA / SEDE (1) --- (N) ACTA
        modelo.Entity<Acta>()
            .HasOne(a => a.Empresa).WithMany(e => e.Actas)
            .HasForeignKey(a => a.IdEmpresa).OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Acta>()
            .HasOne(a => a.Sede).WithMany(s => s.Actas)
            .HasForeignKey(a => a.IdSede).OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Acta>()
            .HasOne(a => a.UsuarioCreador).WithMany(u => u.ActasCreadas)
            .HasForeignKey(a => a.IdUsuarioCreador).OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Acta>().Property(a => a.Tipo).HasConversion<string>().HasMaxLength(20);
        modelo.Entity<Acta>().Property(a => a.Origen).HasConversion<string>().HasMaxLength(20);

        // ACTA (1) --- (N) ASISTENTE_REUNION
        modelo.Entity<AsistenteReunion>()
            .HasOne(a => a.Acta).WithMany(m => m.AsistentesReunion)
            .HasForeignKey(a => a.IdActa).OnDelete(DeleteBehavior.Cascade);

        // ACTA (1) --- (1) CONTENIDO_REUNION
        modelo.Entity<ContenidoReunion>().HasKey(c => c.IdActa);
        modelo.Entity<ContenidoReunion>()
            .HasOne(c => c.Acta).WithOne(a => a.Contenido)
            .HasForeignKey<ContenidoReunion>(c => c.IdActa).OnDelete(DeleteBehavior.Cascade);

        // ACTA (1) --- (N) COMPROMISO_ACTA
        modelo.Entity<CompromisoActa>()
            .HasOne(c => c.Acta).WithMany(a => a.Compromisos)
            .HasForeignKey(c => c.IdActa).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<CompromisoActa>().Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);
        modelo.Entity<CompromisoActa>().Property(c => c.Origen).HasConversion<string>().HasMaxLength(20);
        // DOCUMENTO (1) --- (N) COMPROMISO_ACTA [relacionado, opcional] — el cambio
        // documental que cierra el compromiso; Restrict evita borrar un documento
        // que todavía tiene compromisos de actas apuntándole.
        modelo.Entity<CompromisoActa>()
            .HasOne(c => c.DocumentoRelacionado).WithMany(d => d.CompromisosRelacionados)
            .HasForeignKey(c => c.IdDocumentoRelacionado).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }

    private static void ConfigurarCapacitacionYGamificacion(ModelBuilder modelo)
    {
        modelo.Entity<Curso>().HasKey(c => c.IdCurso);
        modelo.Entity<Leccion>().HasKey(l => l.IdLeccion);
        modelo.Entity<PreguntaQuiz>().HasKey(p => p.IdPregunta);
        modelo.Entity<OpcionQuiz>().HasKey(o => o.IdOpcion);
        modelo.Entity<ProgresoCursoUsuario>().HasKey(p => p.IdProgreso);
        modelo.Entity<SesionJuego>().HasKey(s => s.IdSesionJuego);
        modelo.Entity<Insignia>().HasKey(i => i.IdInsignia);

        // CURSO (1) --- (N) LECCION
        modelo.Entity<Leccion>()
            .HasOne(l => l.Curso).WithMany(c => c.Lecciones)
            .HasForeignKey(l => l.IdCurso).OnDelete(DeleteBehavior.Cascade);

        // CURSO (1) --- (N) PREGUNTA_QUIZ (1) --- (N) OPCION_QUIZ
        modelo.Entity<PreguntaQuiz>()
            .HasOne(p => p.Curso).WithMany(c => c.Preguntas)
            .HasForeignKey(p => p.IdCurso).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<OpcionQuiz>()
            .HasOne(o => o.Pregunta).WithMany(p => p.Opciones)
            .HasForeignKey(o => o.IdPregunta).OnDelete(DeleteBehavior.Cascade);

        // USUARIO (1)---(N) PROGRESO_CURSO_USUARIO (N)---(1) CURSO  [resuelve N:M]
        modelo.Entity<ProgresoCursoUsuario>()
            .HasOne(p => p.Usuario).WithMany(u => u.ProgresoCursos)
            .HasForeignKey(p => p.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<ProgresoCursoUsuario>()
            .HasOne(p => p.Curso).WithMany(c => c.Progreso)
            .HasForeignKey(p => p.IdCurso).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<ProgresoCursoUsuario>().HasIndex(p => new { p.IdUsuario, p.IdCurso }).IsUnique();

        // USUARIO (1) --- (N) SESION_JUEGO
        modelo.Entity<SesionJuego>()
            .HasOne(g => g.Usuario).WithMany(u => u.SesionesJuego)
            .HasForeignKey(g => g.IdUsuario).OnDelete(DeleteBehavior.Cascade);

        // USUARIO (N) --- (N) INSIGNIA, vía INSIGNIA_USUARIO
        modelo.Entity<InsigniaUsuario>().HasKey(iu => new { iu.IdUsuario, iu.IdInsignia });
        modelo.Entity<InsigniaUsuario>()
            .HasOne(iu => iu.Usuario).WithMany(u => u.Insignias)
            .HasForeignKey(iu => iu.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<InsigniaUsuario>()
            .HasOne(iu => iu.Insignia).WithMany(i => i.UsuariosConInsignia)
            .HasForeignKey(iu => iu.IdInsignia).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<Insignia>().HasIndex(i => i.Codigo).IsUnique();
    }

    private static void ConfigurarCalidadYAuditoria(ModelBuilder modelo)
    {
        modelo.Entity<ItemChecklist>().HasKey(i => i.IdItemChecklist);
        modelo.Entity<RegistroAuditoria>().HasKey(r => r.IdRegistroAuditoria);

        // ITEM_CHECKLIST (1) --- (1) RESPUESTA_CHECKLIST
        modelo.Entity<RespuestaChecklist>().HasKey(r => r.IdItemChecklist);
        modelo.Entity<RespuestaChecklist>()
            .HasOne(r => r.ItemChecklist).WithOne(i => i.Respuesta)
            .HasForeignKey<RespuestaChecklist>(r => r.IdItemChecklist).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<RespuestaChecklist>()
            .HasOne(r => r.UsuarioActualiza).WithMany()
            .HasForeignKey(r => r.IdUsuarioActualiza).OnDelete(DeleteBehavior.Restrict);

        // USUARIO (1) --- (N) REGISTRO_AUDITORIA [opcional]
        modelo.Entity<RegistroAuditoria>()
            .HasOne(a => a.Usuario).WithMany(u => u.RegistrosAuditoria)
            .HasForeignKey(a => a.IdUsuario).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
    }
}
