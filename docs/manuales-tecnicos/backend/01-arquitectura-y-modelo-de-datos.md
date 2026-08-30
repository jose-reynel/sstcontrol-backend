# Manual técnico (backend) — Arquitectura y modelo de datos

## Proyectos de la solución

```
SstControl.sln
├── SstControl.Domain           Entidades del dominio, sin dependencias externas
├── SstControl.Application      DTOs + interfaces de servicio (contratos)
├── SstControl.Infrastructure   Implementación de esos servicios + EF Core/PostgreSQL
├── SstControl.Integrations     Conectores a Teams/Google Meet/Zoom/Webex
├── SstControl.Api              Controladores REST, autenticación JWT, RBAC, Program.cs
└── SstControl.Tests            xUnit + EF Core InMemory
```

Arquitectura en capas clásica: `Api` depende de `Application` + `Infrastructure`
+ `Integrations`; `Infrastructure` implementa las interfaces que
`Application` declara; `Domain` no depende de nada (ni siquiera de EF Core).
Todo el código —clases, comentarios, mensajes— está en español; los
namespaces usan grafía castellana (`SstControl.Aplicacion`,
`SstControl.Dominio`, `SstControl.Infraestructura`, `SstControl.Integraciones`,
`SstControl.Api.Controladores`, `SstControl.Api.Seguridad`) aunque las
carpetas físicas conserven el nombre en inglés del proyecto.

## Módulos funcionales y sus tablas

| Módulo | Entidades (tabla EF Core) |
|---|---|
| Control de acceso (RBAC) | `Permiso`, `Perfil`, `PerfilPermiso`, `Rol`, `RolPerfil`, `Usuario`, `UsuarioRol`, `Grupo`, `UsuarioGrupo`, `TokenRenovacion` |
| Organización cliente | `Empresa`, `Sede` |
| Gestión documental | `TipoDocumento`, `Documento`, `DigitalizacionDocumento` |
| Actas y reuniones | `Acta`, `AsistenteReunion`, `ContenidoReunion`, `CompromisoActa` |
| Capacitación y gamificación *(modelo listo, sin API todavía)* | `Curso`, `Leccion`, `PreguntaQuiz`, `OpcionQuiz`, `ProgresoCursoUsuario`, `SesionJuego`, `Insignia`, `InsigniaUsuario` |
| Calidad y auditoría *(modelo listo, sin API todavía)* | `ItemChecklist`, `RespuestaChecklist`, `RegistroAuditoria` |

Los dos últimos módulos **existen en `SstControl.Domain/Entities/Entidades.cs`
y en `ContextoBaseDatos`**, con sus tablas y relaciones ya configuradas, pero
**no tienen controlador** en `SstControl.Api/Controllers` — no son
alcanzables por HTTP todavía. No los uses en integraciones ni los incluyas
en datos semilla de producción hasta que se publique su API.

## Convención de nombres reales en PostgreSQL

EF Core no usa una convención `snake_case` en este proyecto — las tablas y
columnas se llaman **exactamente igual que la propiedad/DbSet en C#**, en
PascalCase. Como PostgreSQL pliega a minúsculas cualquier identificador sin
comillas, **toda tabla y columna requiere comillas dobles** en SQL crudo:

```sql
SELECT "IdUsuario", "NombreUsuario" FROM "Usuarios" WHERE "NombreUsuario" = 'jperez';
```

Los scripts de `docs/datos-semilla/` ya siguen esta convención — úsalos como
referencia si necesitas escribir consultas propias.

## Enums guardados como texto
Estas cuatro propiedades se guardan como `varchar`, no como el entero por
defecto de EF Core (`HasConversion<string>()` en `ContextoBaseDatos`) — para
que la base de datos sea legible directamente sin tener que memorizar qué
número corresponde a qué valor:

| Entidad.Propiedad | Valores posibles |
|---|---|
| `Documento.Estado` | `Pendiente`, `Aprobado` |
| `Acta.Tipo` | `Reunion`, `Capacitacion` |
| `Acta.Origen` | `Manual`, `Teams`, `GoogleMeet`, `Zoom`, `Webex` |
| `CompromisoActa.Estado` | `Pendiente`, `Cumplido` |
| `CompromisoActa.Origen` | `Bot`, `Manual` |

## Relaciones clave a tener presentes
- `Documento` ← (opcional, 1:1) → `DigitalizacionDocumento`: el resultado
  del escaneo OCR. La clave primaria de `DigitalizacionDocumento` **es**
  `IdDocumento` (no tiene su propio autoincremental) — es a la vez PK y FK.
- `Acta` ← (opcional, 1:1) → `ContenidoReunion`: mismo patrón (PK = FK =
  `IdActa`), para el resumen/transcripción de una reunión sincronizada.
- `CompromisoActa.IdDocumentoRelacionado` (opcional): el documento cuyo
  cambio cierra un compromiso — `DeleteBehavior.Restrict`, así que no se
  puede borrar un `Documento` que todavía tiene compromisos apuntándole.
- Borrado en cascada (`DeleteBehavior.Cascade`) en toda la cadena RBAC
  (`Perfil`→`PerfilPermiso`, `Rol`→`RolPerfil`, `Usuario`→`UsuarioRol`,
  etc.) y en `Acta`→`AsistenteReunion`/`ContenidoReunion`/`CompromisoActa`:
  borrar una Acta borra todo lo que cuelga de ella.

## Diagramas de referencia
`mer-sst.mermaid` (raíz del repo) quedó **desactualizado** — corresponde a un
diseño anterior (tablas en inglés, claves UUID) que ya no coincide con el
modelo real en español descrito arriba; lo mismo aplica a `ddl.sql` y
`dml.sql` en la raíz del repo — **no los uses como referencia**. En su lugar,
`docs/manuales-tecnicos/backend/mer-sst-actual.mermaid` (junto a este manual)
sí refleja el modelo real vigente, para los módulos ya expuestos por la API.
