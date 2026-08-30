# Manual de funcionalidad — Administración y control de acceso

## ¿Para qué sirve?
Define **quién puede hacer qué** en el sistema: qué usuarios existen, qué
roles tienen, y qué significa exactamente cada rol en términos de permisos
concretos. Reservado por completo al **Administrador SST** — nadie más ve
ni toca esta sección (permiso `accesos.administrar`).

## Los tres roles del sistema

| Rol | Para quién es | Qué puede hacer |
|---|---|---|
| **Administrador SST** | Quien gestiona la plataforma para la organización | Todo: administrar usuarios/roles, gestionar empresas y sedes, crear/ver actas, firmar y escanear documentos, eliminar documentos, sincronizar reuniones. |
| **Asesor SST** | El profesional de campo que hace el trabajo operativo del día a día | Registrar y ver documentos (cualquier usuario autenticado puede), firmar documentos, escanear evidencia física, crear y ver actas, generar minutas con el bot, sincronizar reuniones. **No** puede administrar usuarios, ni gestionar empresas/sedes, ni eliminar documentos. |
| **Auditor SST** | Quien revisa el cumplimiento, sin operar el sistema | Consultar documentos, actas y compromisos. **No** puede crear, firmar, escanear, eliminar ni administrar nada — su rol es exclusivamente de lectura. |

La jerarquía interna es: **Permiso** (una acción puntual, ej.
`documentos.firmar`) → **Perfil** (un paquete reutilizable de permisos) →
**Rol** (el nombre de negocio, compuesto de uno o varios perfiles) →
**Usuario** (una persona, que puede tener más de un rol a la vez).

## Ver los catálogos
Pantallas **Usuarios**, **Roles**, **Perfiles**, **Permisos** y **Grupos**
(dentro de Administración) — todas de solo consulta desde la aplicación hoy:
muestran cómo está armado cada rol y qué usuarios lo tienen.

## Asignar un rol a un usuario
1. Ve a **Usuarios**.
2. En la fila del usuario, usa el selector "+ Rol…" y elige el rol a
   agregar.
3. Se suma de inmediato — **no reemplaza** los roles que ya tenía; un
   usuario puede combinar más de uno (por ejemplo, ser Asesor SST en una
   empresa y además Administrador SST de la plataforma).

## Asignar un usuario a un grupo
Igual que con roles, pero con el selector "+ Grupo…". Los grupos son solo
organizativos (por ejemplo, "Equipo Constructora Andina") — **no otorgan
permisos por sí solos**, sirven para agrupar usuarios a efectos de reportes.

## Qué falta hoy (para que no te sorprenda)
Crear usuarios nuevos, crear roles/perfiles nuevos, o cambiar qué permisos
tiene un perfil, **no está disponible todavía desde la aplicación** — hoy
esas altas se hacen directamente en la base de datos (ver el manual técnico
de backend y los scripts de datos semilla). Si necesitas dar de alta a
alguien, pide soporte técnico.

## Preguntas frecuentes
**¿Puedo quitarle un rol a un usuario desde la aplicación?**
No todavía — solo agregar. Para quitar un rol, contacta a soporte técnico.

**Un usuario tiene dos roles a la vez, ¿qué permisos tiene?**
La suma de los permisos de todos sus roles — no hay conflicto ni
"restricción" entre roles; si cualquiera de sus roles le da un permiso, lo
tiene.
