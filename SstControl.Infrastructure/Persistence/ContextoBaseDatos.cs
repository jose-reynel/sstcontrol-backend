using Microsoft.EntityFrameworkCore;
using SstControl.Dominio.Entidades;

namespace SstControl.Infraestructura.Persistencia;

/// <summary>
/// Contexto de base de datos (EF Core sobre PostgreSQL). Aquí se configura, con
/// Fluent API, cada relación y cardinalidad definida en el Modelo Entidad-Relación
/// (ver mer-sst.mermaid) — claves foráneas, comportamiento ante borrado, índices únicos.
/// </summary>
public class ContextoBaseDatos : DbContext
{
    public ContextoBaseDatos(DbContextOptions<ContextoBaseDatos> opciones) : base(opciones) { }

    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Sede> Sedes => Set<Sede>();
    public DbSet<TipoDocumento> TiposDocumento => Set<TipoDocumento>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Acta> Actas => Set<Acta>();
    public DbSet<AsistenteReunion> AsistentesReunion => Set<AsistenteReunion>();
    public DbSet<ContenidoReunion> ContenidosReunion => Set<ContenidoReunion>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Leccion> Lecciones => Set<Leccion>();
    public DbSet<PreguntaQuiz> PreguntasQuiz => Set<PreguntaQuiz>();
    public DbSet<OpcionQuiz> OpcionesQuiz => Set<OpcionQuiz>();
    public DbSet<ProgresoCursoUsuario> ProgresoCursoUsuario => Set<ProgresoCursoUsuario>();
    public DbSet<SesionJuego> SesionesJuego => Set<SesionJuego>();
    public DbSet<Insignia> Insignias => Set<Insignia>();
    public DbSet<InsigniaUsuario> InsigniasUsuario => Set<InsigniaUsuario>();
    public DbSet<ItemChecklist> ItemsChecklist => Set<ItemChecklist>();
    public DbSet<RespuestaChecklist> RespuestasChecklist => Set<RespuestaChecklist>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // ---- ROL (1) --- (N) USUARIO ----
        modelo.Entity<Rol>().HasIndex(r => r.Nombre).IsUnique();
        modelo.Entity<Usuario>()
            .HasOne(u => u.Rol).WithMany(r => r.Usuarios)
            .HasForeignKey(u => u.IdRol).OnDelete(DeleteBehavior.Restrict);
        modelo.Entity<Usuario>().HasIndex(u => u.NombreUsuario).IsUnique();

        // ---- EMPRESA (1) --- (N) SEDE ----
        modelo.Entity<Sede>()
            .HasOne(s => s.Empresa).WithMany(e => e.Sedes)
            .HasForeignKey(s => s.IdEmpresa).OnDelete(DeleteBehavior.Cascade);

        // ---- EMPRESA / SEDE (1) --- (N) ACTA ----
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

        // ---- ACTA (1) --- (N) ASISTENTE_REUNION ----
        modelo.Entity<AsistenteReunion>()
            .HasOne(a => a.Acta).WithMany(m => m.AsistentesReunion)
            .HasForeignKey(a => a.IdActa).OnDelete(DeleteBehavior.Cascade);

        // ---- ACTA (1) --- (1) CONTENIDO_REUNION ----
        modelo.Entity<ContenidoReunion>().HasKey(c => c.IdActa);
        modelo.Entity<ContenidoReunion>()
            .HasOne(c => c.Acta).WithOne(a => a.Contenido)
            .HasForeignKey<ContenidoReunion>(c => c.IdActa).OnDelete(DeleteBehavior.Cascade);

        // ---- TIPO_DOCUMENTO (1) --- (N) DOCUMENTO ----
        modelo.Entity<Documento>()
            .HasOne(d => d.TipoDocumento).WithMany(t => t.Documentos)
            .HasForeignKey(d => d.IdTipoDocumento).OnDelete(DeleteBehavior.Restrict);
        // ---- USUARIO (1) --- (N) DOCUMENTO  [aprueba, opcional 0..N] ----
        modelo.Entity<Documento>()
            .HasOne(d => d.UsuarioAprueba).WithMany(u => u.DocumentosAprobados)
            .HasForeignKey(d => d.IdUsuarioAprueba).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        modelo.Entity<Documento>().Property(d => d.Estado).HasConversion<string>().HasMaxLength(20);

        // ---- CURSO (1) --- (N) LECCION ----
        modelo.Entity<Leccion>()
            .HasOne(l => l.Curso).WithMany(c => c.Lecciones)
            .HasForeignKey(l => l.IdCurso).OnDelete(DeleteBehavior.Cascade);

        // ---- CURSO (1) --- (N) PREGUNTA_QUIZ (1) --- (N) OPCION_QUIZ ----
        modelo.Entity<PreguntaQuiz>()
            .HasOne(p => p.Curso).WithMany(c => c.Preguntas)
            .HasForeignKey(p => p.IdCurso).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<OpcionQuiz>()
            .HasOne(o => o.Pregunta).WithMany(p => p.Opciones)
            .HasForeignKey(o => o.IdPregunta).OnDelete(DeleteBehavior.Cascade);

        // ---- USUARIO (1)---(N) PROGRESO_CURSO_USUARIO (N)---(1) CURSO  [resuelve N:M] ----
        modelo.Entity<ProgresoCursoUsuario>()
            .HasOne(p => p.Usuario).WithMany(u => u.ProgresoCursos)
            .HasForeignKey(p => p.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<ProgresoCursoUsuario>()
            .HasOne(p => p.Curso).WithMany(c => c.Progreso)
            .HasForeignKey(p => p.IdCurso).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<ProgresoCursoUsuario>().HasIndex(p => new { p.IdUsuario, p.IdCurso }).IsUnique();

        // ---- USUARIO (1) --- (N) SESION_JUEGO ----
        modelo.Entity<SesionJuego>()
            .HasOne(g => g.Usuario).WithMany(u => u.SesionesJuego)
            .HasForeignKey(g => g.IdUsuario).OnDelete(DeleteBehavior.Cascade);

        // ---- USUARIO (N) --- (N) INSIGNIA  vía INSIGNIA_USUARIO (entidad asociativa) ----
        modelo.Entity<InsigniaUsuario>().HasKey(iu => new { iu.IdUsuario, iu.IdInsignia });
        modelo.Entity<InsigniaUsuario>()
            .HasOne(iu => iu.Usuario).WithMany(u => u.Insignias)
            .HasForeignKey(iu => iu.IdUsuario).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<InsigniaUsuario>()
            .HasOne(iu => iu.Insignia).WithMany(i => i.UsuariosConInsignia)
            .HasForeignKey(iu => iu.IdInsignia).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<Insignia>().HasIndex(i => i.Codigo).IsUnique();

        // ---- ITEM_CHECKLIST (1) --- (1) RESPUESTA_CHECKLIST ----
        modelo.Entity<RespuestaChecklist>().HasKey(r => r.IdItemChecklist);
        modelo.Entity<RespuestaChecklist>()
            .HasOne(r => r.ItemChecklist).WithOne(i => i.Respuesta)
            .HasForeignKey<RespuestaChecklist>(r => r.IdItemChecklist).OnDelete(DeleteBehavior.Cascade);
        modelo.Entity<RespuestaChecklist>()
            .HasOne(r => r.UsuarioActualiza).WithMany()
            .HasForeignKey(r => r.IdUsuarioActualiza).OnDelete(DeleteBehavior.Restrict);

        // ---- USUARIO (1) --- (N) REGISTRO_AUDITORIA [opcional] ----
        modelo.Entity<RegistroAuditoria>()
            .HasOne(a => a.Usuario).WithMany(u => u.RegistrosAuditoria)
            .HasForeignKey(a => a.IdUsuario).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
    }
}
