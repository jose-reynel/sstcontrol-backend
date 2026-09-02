# Manual técnico (backend) — Seguridad, RBAC e integraciones

## Autenticación (JWT + refresh token con rotación)

- `POST /api/autenticacion/iniciar-sesion` — valida usuario/clave (BCrypt) y
  devuelve un JWT de corta duración (`Jwt:MinutosExpiracion`, 480 min por
  defecto) + un token de renovación opaco de larga duración
  (`Jwt:DiasVigenciaTokenRenovacion`, 30 días por defecto).
- `POST /api/autenticacion/renovar-token` — cambia un token de renovación
  vigente por un JWT nuevo **y un token de renovación nuevo** (rotación: el
  usado queda revocado). Si alguien reutiliza un token de renovación ya
  revocado, el sistema revoca **todos** los tokens activos de ese usuario —
  indicio de que el token fue robado y usado por otra parte.
- `POST /api/autenticacion/cerrar-sesion` — revoca el token de renovación en
  el servidor (logout real, no solo borrar el token en el cliente).
- Limitado a **5 intentos por minuto por IP** en login/renovación (política
  de rate limiting `inicio-sesion`, ver `Program.cs`) — mitiga fuerza bruta.

El JWT trae, como claims, el `NameIdentifier` (id de usuario), el `Name`
(nombre completo), un claim `Role` por cada rol del usuario, y un claim
personalizado `permiso` por cada permiso efectivo — la suma de los permisos
de todos los perfiles de todos sus roles.

## RBAC: permisos reales del sistema

Estos son **todos** los códigos de permiso que existen hoy (cualquier otro
código en un `[Authorize(Policy = "...")]` se resolvería dinámicamente pero
simplemente nunca lo tendría nadie — ver
`SstControl.Api/Seguridad/ProveedorPoliticasPermiso.cs`):

| Código | Endpoint(s) que protege |
|---|---|
| `accesos.administrar` | Todo `/api/control-acceso/*` (ver/asignar usuarios, roles, perfiles, permisos, grupos) |
| `empresas.gestionar` | `POST /api/empresas`, `POST /api/empresas/{id}/sedes` |
| `documentos.firmar` | `POST /api/documentos/{id}/firmar` |
| `documentos.eliminar` | `DELETE /api/documentos/{id}` |
| `documentos.escanear` | `POST /api/documentos/{id}/escaneo` |
| `actas.ver` | `GET /api/actas`, `GET /api/actas/{id}/compromisos` |
| `actas.crear` | `POST /api/actas`, `POST /api/actas/{id}/generar-minuta`, `POST /api/actas/{id}/compromisos`, `POST /api/compromisos/{id}/cumplir`, `POST /api/compromisos/{id}/vincular-documento` |
| `reuniones.sincronizar` | Todo `/api/sincronizacion-reuniones/*` y `/api/mapeos-reunion/*` (los webhooks en `/api/webhooks/*` son anónimos y se autentican por firma o token de correlación — ver abajo) |

Endpoints que **solo** requieren `[Authorize]` (cualquier usuario
autenticado, sin permiso específico): `GET/POST /api/documentos`,
`GET /api/documentos/resumen`, `POST /api/documentos/{id}/renovar`,
`GET /api/documentos/{id}/escaneo`, `GET /api/empresas`.

## Diseño de roles usado en los datos semilla

`docs/datos-semilla/01_catalogos_rbac.sql` siembra exactamente esta
jerarquía Permiso → Perfil → Rol:

| Rol | Perfil que lo compone | Permisos incluidos |
|---|---|---|
| **Administrador SST** | Administración total | los 8 permisos completos |
| **Asesor SST** | Gestión documental y de actas | `actas.crear`, `actas.ver`, `documentos.firmar`, `documentos.escanear`, `reuniones.sincronizar` |
| **Auditor SST** | Consulta y auditoría | `actas.ver` (los documentos ya son de consulta libre para cualquier autenticado) |

Para agregar un rol nuevo: crea el `Perfil` con los `PerfilPermiso` que
correspondan, crea el `Rol`, y asócialo con `RolPerfil` — no hay API todavía
para esto (ver manual funcional de Administración), así que se hace por SQL
directo o por una futura pantalla de administración.

## Rate limiting y CORS
- **Login/renovación**: 5 peticiones/minuto por IP (`inicio-sesion`).
- **General**: 200 peticiones/minuto por IP (límite global).
- **CORS**: `Cors:AllowedOrigins` (array) — agrega cada origen exacto
  (protocolo + dominio + puerto) que deba poder llamar la API. No hay
  comodines: un origen no listado se bloquea en el navegador aunque la
  petición llegue con un token válido.

## Integraciones de videollamada (Teams, Google Meet, Zoom, Webex)

Cada conector vive en `SstControl.Integrations/Connectors/` e implementa
`IConectorReunion` — trae reunión + asistentes + contenido (transcripción o
grabación, si la plataforma lo entrega) y los persiste como `Acta` +
`AsistenteReunion` + `ContenidoReunion`.

**Dónde crear cada credencial** (nivel developer de cada plataforma) — el
`README.md` de la raíz del repo, sección *"Capa de integraciones"*, tiene el
paso a paso detallado con capturas de qué botón pulsar en cada portal
(Azure Portal, Zoom Marketplace, Google Cloud Console, developer.webex.com).
Aquí solo el resumen de qué credenciales necesita cada uno en
`Integraciones:*` de `appsettings.json`:

| Plataforma | Tipo de autenticación | Claves en `appsettings.json` |
|---|---|---|
| Teams | Microsoft Graph, permisos de aplicación | `IdInquilino`, `IdCliente`, `ClaveCliente`, `IdUsuarioOrganizador` |
| Zoom | Server-to-Server OAuth | `IdCuenta`, `IdCliente`, `ClaveCliente`, `TokenSecretoWebhook` |
| Google Meet | Cuenta de servicio con delegación de dominio | `CorreoCuentaServicio`, `ClavePrivada`, `CorreoUsuarioSuplantado` |
| Webex | Service App con refresh token de larga duración | `IdCliente`, `ClaveCliente`, `TokenRenovacion`, `TokenSecretoWebhook` |

### Webhooks (sincronización automática al terminar la reunión)
`POST /api/webhooks/{zoom|teams|google-meet|webex}` — `[AllowAnonymous]` a
nivel de autenticación de usuario, porque quien llama es la plataforma
externa, no un usuario de SstControl.

**Convención de correlación empresa/sede** (`MapeoOrigenReunion` — la pieza
que antes quedaba como `TODO`): un webhook de "reunión terminada" no trae de
por sí a qué cliente pertenece esa reunión, así que cada plataforma se
correlaciona con la empresa/sede correcta mediante un **token de
correlación**, elegido por ti al configurar la integración:

| Plataforma | Token de correlación | De dónde sale |
|---|---|---|
| Teams | `clientState` | Un valor libre que tú eliges al crear la suscripción de Microsoft Graph — Graph lo reenvía intacto en cada notificación. |
| Google Meet | `X-Goog-Channel-Token` | Igual que Teams: un valor libre que tú eliges al crear el canal de observación de Calendar — Google lo reenvía en cada notificación. |
| Zoom | Correo del anfitrión (`host_email`) | Zoom no ofrece un campo de correlación libre — se usa el correo del organizador, que sí viene en cada evento. |
| Webex | Correo del anfitrión (`hostEmail`) | Mismo caso que Zoom. |

Configura cada mapeo una vez por cuenta/organizador con
`POST /api/mapeos-reunion` (permiso `reuniones.sincronizar`) — ver
`docs/datos-semilla/05_mapeos_reunion.sql` para ejemplos reales. Si un
webhook llega con un token que no tiene mapeo configurado, el evento se
descarta con una advertencia en el log (nunca falla la petición — la
plataforma no debe reintentar indefinidamente).

Estado de implementación de cada webhook:
- **Zoom y Webex**: sincronización automática completa (resuelven el mapeo y
  llaman a `SincronizarReunionAsync`). Webex además valida la firma
  `X-Spark-Signature` (HMAC-SHA1, comparación en tiempo constante) antes de
  confiar en el evento; **Zoom todavía no valida `x-zm-signature`** — queda
  como el único `TODO` de seguridad real pendiente en este flujo, aplicando
  el mismo patrón que ya tiene Webex.
- **Teams**: sincronización automática completa, iterando cada notificación
  del array `value[]` que envía Microsoft Graph.
- **Google Meet**: resuelve el mapeo (sabe a qué empresa/sede pertenece el
  canal), pero **no determina qué reunión cambió** — es una limitación real
  de la plataforma: Calendar push notifications solo avisan "algo cambió",
  sin traer el detalle; hace falta una llamada de seguimiento a
  `events.list` con el `syncToken` guardado del canal, que queda fuera de
  este alcance. Mientras tanto, usa la sincronización manual
  (`POST /api/sincronizacion-reuniones/googlemeet`) para Google Meet.

## OCR (Tesseract)
`ServicioOcrTesseract` corre 100% local — sin API de nube, sin credenciales
externas. Solo acepta imágenes rasterizadas (JPEG, PNG, BMP, TIFF; máx. 15
MB) — nunca PDF directamente. Requiere el binario nativo `tesseract-ocr` +
los datos de idioma instalados en el servidor (ver
`03-despliegue-en-servidor-de-aplicaciones.md`); si el binario no está
instalado, `ServicioOcrTesseract` falla en tiempo de ejecución con un error
claro, no al compilar.
