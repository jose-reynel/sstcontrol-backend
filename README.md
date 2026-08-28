# SstControl — Backend (.NET 10 + EF Core + PostgreSQL)

> **Convención de idioma:** todo el código (clases, propiedades, métodos, parámetros,
> variables) está en **español**. Los nombres de proyecto/carpeta (`SstControl.Domain`,
> `SstControl.Application`, etc.) se mantienen en inglés porque son la ruta física de
> los `.csproj` — cambiarlos requeriría renombrar carpetas y el `.sln`, algo puramente
> mecánico sin valor real. Los **namespaces internos sí están en español**
> (`SstControl.Dominio.Entidades`, `SstControl.Aplicacion.Interfaces`, etc.) y cada
> clase/método clave tiene un comentario `///` explicando su función.

Arquitectura en 4 capas:

```
SstControl.Domain          → Entidades puras (sin dependencias externas)
SstControl.Application     → DTOs + interfaces (puertos) de servicios y repositorios
SstControl.Infrastructure  → EF Core, DbContext, repositorios, servicios, PostgreSQL (Npgsql)
SstControl.Api             → Controladores REST, JWT, Program.cs, Swagger
```

Domain no depende de nada. Application depende solo de Domain. Infrastructure implementa
las interfaces de Application. Api orquesta todo por inyección de dependencias
(`Program.cs`). Así el dominio nunca queda acoplado a PostgreSQL ni a ASP.NET.

## Requisitos
- .NET 10 SDK
- Docker (para levantar PostgreSQL fácilmente) o una instancia PostgreSQL propia

## 1. Levantar PostgreSQL
```bash
docker compose up -d postgres
```

## 2. Crear la primera migración y aplicarla
```bash
cd SstControl.Api
dotnet tool install --global dotnet-ef   # una sola vez
dotnet ef migrations add InitialCreate --project ../SstControl.Infrastructure --startup-project .
dotnet ef database update --project ../SstControl.Infrastructure --startup-project .
```
Esto crea todas las tablas del MER (`mer-sst.mermaid`) en PostgreSQL respetando las
cardinalidades configuradas en `SstControlDbContext.OnModelCreating`.

## 3. Configurar credenciales

**Desarrollo local (sin Docker):** copia `SstControl.Api/appsettings.Development.json.example`
a `SstControl.Api/appsettings.Development.json` (ignorado por git) y ajusta los valores.

**Con `docker compose`:** copia `.env.example` a `.env` (ignorado por git) y completa
`POSTGRES_PASSWORD`, `JWT_KEY` (mínimo 32 caracteres — genera uno con `openssl rand -base64 48`)
y `CORS_ORIGEN_PRINCIPAL`. `docker-compose.yml` ya no trae contraseñas ni llaves por defecto:
sin `.env` completo, `docker compose up` falla explícitamente en vez de arrancar con un
secreto débil.

- `Cors:AllowedOrigins` acepta **varios orígenes** (array) — agrega ahí tanto tu dominio
  de producción como los puertos de desarrollo del frontend (Web y emulador Maui).
- `Jwt:MinutosExpiracion` controla cuánto dura el JWT antes de necesitar renovarse
  (por defecto 480 = 8 horas) y `Jwt:DiasVigenciaTokenRenovacion` cuánto dura la
  sesión completa antes de pedir la contraseña de nuevo (por defecto 30 días).

**Nunca subas `appsettings.json` con claves reales a un repo público** — usa
`dotnet user-secrets` en desarrollo o variables de entorno en producción.

## 4. Ejecutar la API
```bash
dotnet run --project SstControl.Api
```
Swagger disponible en `https://localhost:xxxx/swagger`. Estado de salud (usado por
`docker-compose.yml` y por cualquier orquestador) en `GET /salud`.

## 5. Frontend

El cliente de esta API es **[sstcontrol-frontend](https://github.com/jose-reynel/sstcontrol-frontend)**
— una app Blazor (.NET 10) compartida entre Web (WebAssembly/PWA) y Mobile/Desktop
(.NET MAUI Blazor Hybrid: Android, iOS, macOS, Windows). Los DTOs de
`SstControl.Application/DTOs/Dtos.cs` son el contrato — cualquier cambio ahí debe
reflejarse en `SstControl.Frontend.Shared/Models` del frontend.

## Robustecimiento técnico (observabilidad, seguridad, escalabilidad)

Además del CRUD funcional, la API incluye:

- **Manejo global de errores** (`SstControl.Api/Middleware/ManejadorErroresGlobal.cs`):
  toda excepción no controlada se traduce a `application/problem+json` (RFC 7807) con
  un `idRastreo` correlacionable con los logs — nunca un HTML genérico de error 500.
- **Logging estructurado** con Serilog (JSON a consola, listo para conectar a Seq,
  Grafana Loki, ELK o Datadog) — configurable por `appsettings.json` → `Serilog`.
- **Health check real** en `GET /salud`: verifica conectividad efectiva a PostgreSQL
  (no solo que el proceso esté vivo), en JSON.
- **Rate limiting**: 5 intentos por minuto por IP en `POST /api/autenticacion/iniciar-sesion`
  (mitiga fuerza bruta de credenciales) y un límite general de 200 peticiones/minuto por IP.
- **Validación de entrada** (`DataAnnotations` en los DTOs `Crear*`): la API responde 400
  con `ValidationProblemDetails` antes de tocar la base de datos si faltan campos o exceden
  longitudes razonables.
- **Paginación** en `GET /api/documentos` y `GET /api/actas` (`?pagina=1&tamanioPagina=20`,
  máximo 100 por página) — evita traer la tabla completa a medida que crece el histórico.
- **Resumen agregado** en `GET /api/documentos/resumen` (total, pendientes, vencidos,
  aprobados) calculado con `COUNT` en la base de datos — para paneles, sin traer
  los documentos en sí.
- **CORS multi-origen** configurable por array, no un solo string fijo.
- **Refresh tokens con rotación** (`TokenRenovacion`, tabla nueva — genera la
  migración con `dotnet ef migrations add`): el login devuelve un JWT de corta
  duración + un token de renovación opaco de larga duración. `POST
  /api/autenticacion/renovar-token` cambia un token de renovación vigente por
  un JWT nuevo (y un token de renovación nuevo — el usado queda revocado). Si
  alguien reutiliza un token de renovación ya revocado (señal de robo), se
  revocan TODOS los tokens activos de ese usuario. `POST
  /api/autenticacion/cerrar-sesion` revoca el token del lado del servidor —
  antes "cerrar sesión" solo borraba el token en el cliente.
- **Tests automatizados** (`SstControl.Tests`, xUnit + EF Core InMemory):
  cubren la paginación y el resumen agregado de Documentos, el flujo completo
  de refresh tokens (emisión, rotación, y la revocación en cadena ante reuso de
  un token ya rotado), y las validaciones de digitalización OCR que no
  requieren el binario nativo de Tesseract instalado. Corre `dotnet test`
  desde la raíz del repo.

Pendiente conocido, no implementado todavía: **versionado de rutas de API**
(`/api/v2/...` cuando haya un cambio incompatible).


## Alcance de este scaffold
Se implementaron completos: **Auth (con refresh tokens), Documentos (con OCR),
Empresas/Sedes, Actas (con bot de minutas y compromisos), Control de acceso RBAC**.
El resto de entidades del MER (Cursos, Progreso, Gamificación, Checklist, Bitácora)
ya están modeladas en `ContextoBaseDatos` y siguen exactamente el mismo patrón
(Repositorio → Servicio → Controlador) — solo falta repetir la receta.

## 6. Bot de minutas: seguimiento de actas y su vínculo con Documentos

Da seguimiento real a una Acta más allá del registro de la reunión: extrae
**compromisos** (acuerdo, responsable, fecha límite) del contenido ya
sincronizado, y permite vincular cada uno al **Documento** cuyo cambio lo cierra
— la pieza que conecta "qué se acordó en la reunión" con "qué cambió en el
sistema documental".

- `IServicioResumenReunion` (puerto de extracción) → `ServicioResumenReunionHeuristico`,
  la implementación por defecto: reglas de texto (palabras clave como
  "acción:", "compromiso:", "responsable:", casillas `- [ ]`), **no** un modelo
  de lenguaje — es intencionalmente simple y gratuita, pensada como un primer
  barrido para revisar y corregir a mano. Conectar un proveedor de IA real (ej.
  la API de Anthropic) más adelante es escribir otra implementación de esta
  misma interfaz y cambiar un registro en `Program.cs`, sin tocar nada más.
- `POST /api/actas/{id}/generar-minuta` — corre el bot sobre `ContenidoReunion.Resumen`
  (transcripción o resumen que el conector de Teams/Meet/Zoom/Webex ya dejó
  guardado) y registra los compromisos nuevos. Idempotente: no duplica
  compromisos que el bot ya había generado sobre el mismo texto.
- `GET /api/actas/{id}/compromisos`, `POST /api/actas/{id}/compromisos` (agregar a mano).
- `POST /api/compromisos/{id}/cumplir`, `POST /api/compromisos/{id}/vincular-documento`.

## 7. Digitalización (OCR) de documentos físicos

`POST /api/documentos/{id}/escaneo` (multipart/form-data, campo `archivo`) sube
una foto o imagen escaneada de un documento físico y ejecuta OCR **local** con
[Tesseract](https://github.com/tesseract-ocr/tesseract) — sin depender de una
API de nube ni de credenciales externas. Solo el texto reconocido y su
confianza se guardan (`DigitalizacionDocumento`); la imagen en sí se procesa en
memoria y se descarta.

- Formatos soportados: JPEG, PNG, BMP, TIFF — máximo 15 MB. Un PDF debe
  convertirse a imagen antes de subirlo (fuera de alcance a propósito, para no
  sumar una dependencia de conversión como Ghostscript).
- Requiere el permiso `documentos.escanear` (RBAC) — como cualquier permiso
  nuevo, hace falta sembrarlo en la tabla `Permiso` y asignarlo a los roles
  correspondientes; no hay un seeder automático todavía (ver limitación
  conocida más abajo).
- El binario nativo y los datos de idioma español ya vienen instalados en la
  imagen Docker (`SstControl.Api/Dockerfile`). Para correr fuera de Docker,
  instala `tesseract-ocr` + el paquete de idioma español de tu SO y ajusta
  `Ocr:RutaDatosEntrenamiento` en `appsettings.json`.
- `GET /api/documentos/{id}/escaneo` — consulta la digitalización ya hecha (o `null`).

## 8. Capa de integraciones: Teams, Google Meet, Zoom y Webex

Proyecto **`SstControl.Integrations`**, con un conector developer-level por
plataforma que implementa la interfaz común `IConectorReunion` (Application):

```
SstControl.Integrations/
 ├── Auth/ProveedorTokenClientCredentials.cs  → OAuth2 client-credentials (Teams, Zoom)
 └── Connectors/
      ├── ConectorTeams.cs         → Microsoft Graph (Azure AD app, permisos Application)
      ├── ConectorZoom.cs          → Zoom Server-to-Server OAuth
      ├── ConectorGoogleMeet.cs    → Google Meet REST API (cuenta de servicio, JWT RS256)
      ├── ConectorWebex.cs         → Cisco Webex (Service App, refresh token de larga duración)
      └── FabricaConectoresReunion.cs
```

`ServicioSincronizacionReuniones` (Infrastructure) orquesta: llama al conector →
trae reunión + asistentes + contenido (transcripción/grabación si existe) →
persiste como `Acta` + `AsistenteReunion` (uno por asistente, con hora de
entrada/salida) + `ContenidoReunion` en PostgreSQL. Ese `ContenidoReunion` es
justo el insumo que después consume el bot de minutas (sección 6).

### Qué debes crear en cada plataforma (nivel developer)

**Microsoft Teams (Microsoft Graph)**
1. [portal.azure.com](https://portal.azure.com) → App registrations → New registration.
2. Genera un **Client Secret**.
3. En "API permissions" agrega permisos de tipo **Application** (no delegados):
   `OnlineMeetings.Read.All`, `OnlineMeetingArtifact.Read.All` → pide **consentimiento
   de administrador** (botón "Grant admin consent").
4. Copia `TenantId`, `ClientId`, `ClientSecret` y el `UPN`/id del organizador a
   `appsettings.json` → `Integraciones:Teams`.

**Zoom (Server-to-Server OAuth)**
1. [marketplace.zoom.us](https://marketplace.zoom.us) → Develop → Build App → **Server-to-Server OAuth**.
2. Scopes: `meeting:read:admin`, `report:read:admin` (y `recording:read:admin` si quieres grabaciones).
3. Copia `Account ID`, `Client ID`, `Client Secret` a `Integraciones:Zoom`.
4. Para el webhook: en la misma app, sección "Event Subscriptions", registra
   `https://tu-api.com/api/webhooks/zoom` y copia el **Secret Token** a `Integraciones:Zoom:TokenSecretoWebhook`.

**Google Meet (Google Meet REST API)**
1. [console.cloud.google.com](https://console.cloud.google.com) → crea proyecto → habilita **"Google Meet API"**.
2. IAM & Admin → Service Accounts → crea una, genera clave **JSON**.
3. En [admin.google.com](https://admin.google.com) (Workspace, requiere ser administrador
   del dominio) → Seguridad → Controles de API → Delegación en todo el dominio → autoriza
   el `client_id` de la cuenta de servicio con el scope
   `https://www.googleapis.com/auth/meetings.space.readonly`.
4. Copia `client_email` y `private_key` del JSON a `Integraciones:GoogleMeet`.

**Cisco Webex (Service App)**
1. [developer.webex.com](https://developer.webex.com) → My Webex Apps → **Create a Service App**.
2. Scopes: `meeting:schedules_read`, `meeting:participants_read`,
   `meeting:recordings_read`, `meeting:transcripts_read`.
3. Un administrador de tu organización Webex debe **activar** la Service App
   (no queda funcional solo con crearla).
4. Webex entrega `client_id`/`client_secret` + un **refresh_token** de larga
   duración — cópialos a `Integraciones:Webex`.
5. Para el webhook: `POST /v1/webhooks` en la API de Webex, con
   `targetUrl = https://tu-api.com/api/webhooks/webex`, `resource = meetings`,
   `event = ended`, y un `secret` propio — copia ese mismo secret a
   `Integraciones:Webex:TokenSecretoWebhook`. A diferencia de Zoom/Teams, Webex
   no exige un reto de validación al registrar el webhook; en cambio, **cada
   evento llega firmado** con HMAC-SHA1 en el header `X-Spark-Signature`, y el
   endpoint sí valida esa firma (comparación en tiempo constante) antes de
   procesar cualquier evento — a diferencia de los otros tres webhooks, que
   por ahora solo resuelven el reto de registro.

### Endpoints expuestos por la API

```
POST /api/sincronizacion-reuniones/{proveedor}   { idReunionExterna, idEmpresa, idSede, tipo }
                                                  proveedor: teams | googlemeet | zoom | webex
POST /api/webhooks/teams        ← registrar en Graph change notifications
POST /api/webhooks/zoom         ← registrar en Zoom Event Subscriptions
POST /api/webhooks/google-meet  ← receptor si usas Pub/Sub push a HTTP
POST /api/webhooks/webex        ← registrar vía POST /v1/webhooks de Webex (firma validada)
```

La sincronización manual requiere el permiso `reuniones.sincronizar`; los
cuatro webhooks son anónimos por diseño (los llama la plataforma externa, no
un usuario autenticado de SstControl) — Zoom/Webex validan la petición por su
cuenta (reto de registro o firma); Teams/Google Meet todavía no.

### Limitación honesta
En los cuatro webhooks, el procesamiento del evento real de "reunión terminada"
queda marcado con `TODO`: mapear automáticamente una reunión externa a una
`Empresa`/`Sede` del sistema requiere una convención propia tuya (ej. un código
de sede en el título de la reunión, o una tabla `sede ↔ id de sala/organizador`)
que no puedo inventar sin conocer cómo organizas tus reuniones reales. La
sincronización manual (`POST /api/sincronizacion-reuniones/{proveedor}`) sí
queda 100% funcional mientras tanto.
