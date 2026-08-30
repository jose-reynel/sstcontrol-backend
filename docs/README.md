# Documentación de SstControl

Índice general. Todo aquí describe **exactamente** lo que el sistema hace hoy —
ningún manual documenta una funcionalidad que no esté realmente implementada
y expuesta por la API (`SstControl.Api/Controllers`). El módulo de
Capacitación/Gamificación y el de Calidad/Auditoría existen en el modelo de
datos (`SstControl.Domain/Entities/Entidades.cs`) pero **todavía no tienen
controladores** — por eso no aparecen en estos manuales ni en los datos
semilla: documentarlos ahora sería documentar algo que un usuario real no
puede usar.

## 1. Manuales de funcionalidad
Qué hace el sistema y cómo se usa, sin detalles de implementación — para
Asesores SST, Administradores SST y Auditores SST.

- [`manuales-funcionalidad/01-autenticacion-y-sesion.md`](manuales-funcionalidad/01-autenticacion-y-sesion.md)
- [`manuales-funcionalidad/02-empresas-y-sedes.md`](manuales-funcionalidad/02-empresas-y-sedes.md)
- [`manuales-funcionalidad/03-gestion-documental.md`](manuales-funcionalidad/03-gestion-documental.md)
- [`manuales-funcionalidad/04-actas-reuniones-y-bot-de-minutas.md`](manuales-funcionalidad/04-actas-reuniones-y-bot-de-minutas.md)
- [`manuales-funcionalidad/05-administracion-control-de-acceso.md`](manuales-funcionalidad/05-administracion-control-de-acceso.md)
- [`manuales-funcionalidad/06-integraciones-videollamadas.md`](manuales-funcionalidad/06-integraciones-videollamadas.md)

## 2. Manuales técnicos
Cómo instalar, configurar, parametrizar y desplegar cada componente en un
servidor de aplicaciones real — separados por capa.

**Backend** (`manuales-tecnicos/backend/`, en este repositorio)
- [`01-arquitectura-y-modelo-de-datos.md`](manuales-tecnicos/backend/01-arquitectura-y-modelo-de-datos.md)
- [`02-instalacion-y-configuracion.md`](manuales-tecnicos/backend/02-instalacion-y-configuracion.md)
- [`03-despliegue-en-servidor-de-aplicaciones.md`](manuales-tecnicos/backend/03-despliegue-en-servidor-de-aplicaciones.md)
- [`04-seguridad-rbac-e-integraciones.md`](manuales-tecnicos/backend/04-seguridad-rbac-e-integraciones.md)

**Frontend** (repositorio [`sstcontrol-frontend`](https://github.com/jose-reynel/sstcontrol-frontend/tree/main/docs) — separado a propósito, junto al código que documenta)
- [`01-arquitectura.md`](https://github.com/jose-reynel/sstcontrol-frontend/blob/main/docs/01-arquitectura.md)
- [`02-instalacion-y-configuracion.md`](https://github.com/jose-reynel/sstcontrol-frontend/blob/main/docs/02-instalacion-y-configuracion.md)
- [`03-despliegue-web-y-mobile.md`](https://github.com/jose-reynel/sstcontrol-frontend/blob/main/docs/03-despliegue-web-y-mobile.md)

## 3. Capacitación
Material para formar a los tres perfiles de usuario del sistema.

- [`capacitacion/plan-de-capacitacion.md`](capacitacion/plan-de-capacitacion.md)
- [`capacitacion/caso-de-estudio-end-to-end.md`](capacitacion/caso-de-estudio-end-to-end.md) — el ejemplo real
  que recorre las funcionalidades de punta a punta.

## 4. Datos semilla y transacciones simuladas
Scripts SQL para poblar cada catálogo del sistema y simular un escenario
completo de uso: **5 empresas, 10 Asesores SST, 3 Administradores SST y 2
Auditores SST**, con documentos, actas, compromisos y reuniones sincronizadas
de ejemplo.

- [`datos-semilla/LEEME.md`](datos-semilla/LEEME.md) — orden de ejecución y prerrequisitos.
- `datos-semilla/01_catalogos_rbac.sql`
- `datos-semilla/02_empresas_y_sedes.sql`
- `datos-semilla/03_usuarios_y_asignacion_roles.sql`
- `datos-semilla/04_transacciones_simuladas.sql`
