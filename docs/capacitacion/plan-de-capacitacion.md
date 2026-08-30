# Plan de capacitación — SstControl

## Objetivo
Que cada perfil de usuario (Administrador SST, Asesor SST, Auditor SST)
quede autónomo en las funciones que le corresponden, sin necesitar soporte
para el uso diario del sistema.

## A quién capacitar y en qué

| Sesión | Dirigida a | Duración sugerida | Contenido |
|---|---|---|---|
| 1. Fundamentos | Los tres perfiles, juntos | 45 min | Iniciar/cerrar sesión, navegación general, qué ve cada rol y por qué (RBAC en términos simples, sin tecnicismos). |
| 2. Gestión documental | Asesor SST + Administrador SST | 60 min | Registrar, firmar, renovar y escanear documentos. Interpretar el resumen del Panel. |
| 3. Actas y bot de minutas | Asesor SST + Administrador SST | 60 min | Registrar actas a mano, sincronizar desde videollamada, generar minutas con el bot, dar seguimiento a compromisos, vincularlos a documentos. |
| 4. Auditoría | Auditor SST | 30 min | Qué puede y qué no puede hacer el rol; cómo consultar documentos/actas/compromisos con criterio de auditoría (qué mirar, qué reportar). |
| 5. Administración | Administrador SST | 45 min | Empresas/sedes, usuarios y roles, qué está disponible desde la app y qué requiere soporte técnico. |

Usa el **caso de estudio end to end** (`caso-de-estudio-end-to-end.md`) como
hilo conductor de las sesiones 2 y 3 — es un escenario real, no ejercicios
sueltos, para que cada acción se entienda en contexto.

## Material de apoyo
Los manuales de funcionalidad (`docs/manuales-funcionalidad/`) son la
referencia de consulta posterior a la capacitación — cada sesión debería
cerrar señalando cuál manual leer para profundizar.

## Ambiente de práctica
Antes de capacitar en el ambiente productivo real, aplica los scripts de
`docs/datos-semilla/` sobre un ambiente de pruebas — crean 5 empresas, 20
usuarios (3 Administradores SST, 10 Asesores SST, 2 Auditores SST — más
otros roles de referencia) y decenas de documentos/actas/compromisos ya
cargados, para que cada participante practique con datos realistas sin
arriesgar información real de un cliente.

### Credenciales de práctica
Todos los usuarios semilla comparten la misma contraseña de práctica:
**`Practica#2026`** (ver `docs/datos-semilla/03_usuarios_y_asignacion_roles.sql`
para el detalle exacto). **Cámbiala antes de reutilizar este mismo script en
un ambiente que deje de ser exclusivamente de práctica.**

## Evaluación de la capacitación
Sugerido: al cierre de cada sesión, pide a cada participante completar en
vivo, sobre el ambiente de práctica, una acción real de su rol (ej. un
Asesor SST firma un documento pendiente y genera una minuta con el bot; un
Auditor SST identifica cuántos documentos están vencidos hoy). Verificar que
lo logren solos es la evaluación — no un examen aparte.
