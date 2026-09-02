# Datos semilla — orden de ejecución

## Prerrequisitos
1. Base de datos migrada (`dotnet ef database update` — ver
   `docs/manuales-tecnicos/backend/02-instalacion-y-configuracion.md`). Estos
   scripts asumen que las tablas ya existen; no las crean.
2. Ejecutar **en este orden exacto** (cada uno depende de que el anterior ya
   haya corrido, por nombre — no por ID):
   ```bash
   psql -U sst_user -d sst_control -f 01_catalogos_rbac.sql
   psql -U sst_user -d sst_control -f 02_empresas_y_sedes.sql
   psql -U sst_user -d sst_control -f 03_usuarios_y_asignacion_roles.sql
   psql -U sst_user -d sst_control -f 04_transacciones_simuladas.sql
   psql -U sst_user -d sst_control -f 05_mapeos_reunion.sql
   ```

## Qué siembra cada script

| Script | Contenido |
|---|---|
| `01_catalogos_rbac.sql` | 8 Permisos, 3 Perfiles, 3 Roles (Administrador SST / Asesor SST / Auditor SST) con sus permisos ya vinculados, y 5 Tipos de Documento. |
| `02_empresas_y_sedes.sql` | 5 empresas cliente con 9 sedes en total. |
| `03_usuarios_y_asignacion_roles.sql` | 15 usuarios — **3 Administradores SST, 10 Asesores SST, 2 Auditores SST** — cada uno con su rol asignado y agrupado organizativamente a la empresa donde trabaja. |
| `04_transacciones_simuladas.sql` | Actividad ficticia end-to-end: **39 documentos** (18 aprobados, 11 de ellos vencidos por no haberse renovado a tiempo — mezcla deliberada), **15 actas** (6 manuales + 9 sincronizadas: 2 Teams, 2 Zoom, 2 Google Meet, 3 Webex), **18 asistentes de reunión**, **9 contenidos de reunión** (resúmenes con marcadores que el bot reconoce), **20 compromisos** (17 generados por el bot + 3 manuales; 3 vinculados a un documento real), **6 digitalizaciones OCR** de ejemplo, y **10 registros de auditoría**. |
| `05_mapeos_reunion.sql` | **8 mapeos** origen→empresa/sede (2 por empresa) — la configuración que permite que los webhooks de Teams/Google Meet/Zoom/Webex sepan a qué cliente pertenece cada reunión que sincronizan automáticamente. Ver el manual técnico de seguridad/RBAC/integraciones para el mecanismo real de cada plataforma. Idempotente. |

> Estos números se verificaron corriendo los 4 scripts contra una instancia
> real de PostgreSQL 16, sobre un esquema reconstruido a mano fiel al modelo
> de `SstControl.Domain/Entities/Entidades.cs` (no se pudo generar la
> migración real de EF Core en el entorno donde se escribió este documento,
> por falta de acceso a NuGet) — no son una estimación.

## Cómo escriben las FK estos scripts
Ningún script asume un `Id` autogenerado específico — cada inserción
resuelve sus claves foráneas por **nombre/código único** (`WHERE "Nombre" =
'...'`, `WHERE "Codigo" = '...'`), así que son seguros de correr sobre una
base de datos recién creada sin preocuparte por qué IDs asignó Postgres.

## Cuáles scripts son idempotentes (se pueden correr más de una vez)

Verificado corriendo cada script dos veces seguidas contra PostgreSQL real:

| Script | ¿Se puede correr dos veces sin duplicar? | Por qué |
|---|---|---|
| `01_catalogos_rbac.sql` | **Sí** | `ON CONFLICT DO NOTHING` sobre los índices únicos de `Permiso.Codigo`, `Perfil.Nombre`, `Rol.Nombre`; `TipoDocumento` (sin índice único) se protege con `WHERE NOT EXISTS`. |
| `02_empresas_y_sedes.sql` | No | `Empresa.Nombre` no tiene índice único — correrlo dos veces duplica las 5 empresas. |
| `03_usuarios_y_asignacion_roles.sql` | **Sí** | `ON CONFLICT DO NOTHING` sobre `Usuario.NombreUsuario` y las claves compuestas de `UsuarioRoles`/`UsuarioGrupos`; `Grupos` (sin índice único) se protege con `WHERE NOT EXISTS`. |
| `04_transacciones_simuladas.sql` | No | Pensado para correrse una sola vez — resuelve sus FK por título de acta / nombre de colaborador, que no son únicos a nivel de base de datos. |
| `05_mapeos_reunion.sql` | **Sí** | `ON CONFLICT DO NOTHING` sobre el índice único `("Origen", "TokenCorrelacion")`. |

## Credenciales de todos los usuarios semilla
Contraseña única para practicar: **`Practica#2026`** (hash BCrypt real,
verificado — no un marcador de posición). El nombre de usuario de cada
persona sigue el patrón `nombre.apellido` (ver
`03_usuarios_y_asignacion_roles.sql` para el listado completo).

**Antes de usar este mismo script fuera de un ambiente exclusivamente de
práctica**, cambia la contraseña de cada usuario (o mejor, no reutilices
estos scripts en producción: son para capacitación y demostración).

## Volver a correr desde cero
Estos scripts **no son idempotentes** — correrlos dos veces duplica los
datos o falla por violación de una restricción única (`Codigo`/`Nombre`
duplicado), según la tabla. Si necesitas reiniciar el ambiente de práctica,
trunca las tablas afectadas (`TRUNCATE ... RESTART IDENTITY CASCADE`) o
recrea la base de datos entera antes de volver a correr los cuatro scripts.
