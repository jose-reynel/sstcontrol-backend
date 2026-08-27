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
  cubren la paginación y el resumen agregado de Documentos, y el flujo completo
  de refresh tokens (emisión, rotación, y la revocación en cadena ante reuso de
  un token ya rotado). Corre `dotnet test` desde la raíz del repo.

Pendiente conocido, no implementado todavía: **versionado de rutas de API**
(`/api/v2/...` cuando haya un cambio incompatible).


## Alcance de este scaffold
Se implementaron completos como ejemplo: **Auth, Documentos, Empresas/Sedes, Actas**.
El resto de entidades del MER (Cursos, Progreso, Gamificación, Checklist, Bitácora)
ya están modeladas en `SstControlDbContext` y siguen exactamente el mismo patrón
(Repositorio → Servicio → Controlador) — solo falta repetir la receta.

## 6. Capa de integraciones: Teams, Google Meet y Zoom

Nuevo proyecto **`SstControl.Integrations`**, con un conector developer-level por
plataforma que implementa la interfaz común `IMeetingConnector` (Application):

```
SstControl.Integrations/
 ├── Auth/ClientCredentialsTokenProvider.cs   → OAuth2 client-credentials (Teams, Zoom)
 └── Connectors/
      ├── ConectorTeams.cs         → Microsoft Graph (Azure AD app, permisos Application)
      ├── ConectorZoom.cs          → Zoom Server-to-Server OAuth
      ├── ConectorGoogleMeet.cs    → Google Meet REST API (cuenta de servicio, JWT RS256)
      └── MeetingConnectorFactory.cs
```

`MeetingSyncService` (Infrastructure) orquesta: llama al conector → trae reunión +
asistentes + contenido (grabación/resumen si existe) → persiste como `Minute` +
`MeetingAttendee` (uno por asistente, con hora de entrada/salida) + `MeetingContent`
en PostgreSQL, y deja registro en `AuditLog`. Es idempotente: si la reunión ya se
había sincronizado, actualiza en vez de duplicar.

### Qué debes crear en cada plataforma (nivel developer)

**Microsoft Teams (Microsoft Graph)**
1. [portal.azure.com](https://portal.azure.com) → App registrations → New registration.
2. Genera un **Client Secret**.
3. En "API permissions" agrega permisos de tipo **Application** (no delegados):
   `OnlineMeetings.Read.All`, `OnlineMeetingArtifact.Read.All` → pide **consentimiento
   de administrador** (botón "Grant admin consent").
4. Copia `TenantId`, `ClientId`, `ClientSecret` a `appsettings.json` → `Integrations:Teams`.

**Zoom (Server-to-Server OAuth)**
1. [marketplace.zoom.us](https://marketplace.zoom.us) → Develop → Build App → **Server-to-Server OAuth**.
2. Scopes: `meeting:read:admin`, `report:read:admin` (y `recording:read:admin` si quieres grabaciones).
3. Copia `Account ID`, `Client ID`, `Client Secret` a `Integrations:Zoom`.
4. Para el webhook: en la misma app, sección "Event Subscriptions", registra
   `https://tu-api.com/api/webhooks/zoom` y copia el **Secret Token** a `Integrations:Zoom:WebhookSecretToken`.

**Google Meet (Google Meet REST API)**
1. [console.cloud.google.com](https://console.cloud.google.com) → crea proyecto → habilita **"Google Meet API"**.
2. IAM & Admin → Service Accounts → crea una, genera clave **JSON**.
3. En [admin.google.com](https://admin.google.com) (Workspace, requiere ser administrador
   del dominio) → Seguridad → Controles de API → Delegación en todo el dominio → autoriza
   el `client_id` de la cuenta de servicio con el scope
   `https://www.googleapis.com/auth/meetings.space.readonly`.
4. Copia `client_email` y `private_key` del JSON a `Integrations:GoogleMeet`.

### Endpoints expuestos por la API

```
POST /api/meetingsync/teams        { externalMeetingId, companyId, siteId, type }
POST /api/meetingsync/zoom         { externalMeetingId, companyId, siteId, type }
POST /api/meetingsync/googlemeet   { externalMeetingId, companyId, siteId, type }
POST /api/webhooks/teams           ← registrar en Graph change notifications
POST /api/webhooks/zoom            ← registrar en Zoom Event Subscriptions
POST /api/webhooks/google-meet     ← receptor si usas Pub/Sub push a HTTP
```

Los tres endpoints de sincronización requieren rol `admin` (mismo JWT del resto de la API).

### Limitación honesta
Los webhooks de Teams y Zoom quedan con el **punto de entrada y la validación de registro
ya resueltos** (Zoom "url_validation", Graph "validationToken"), pero el procesamiento del
evento real (`meeting.ended`, notificación de Graph) queda marcado con `TODO` — mapear
automáticamente una reunión externa a una `Company`/`Site` del sistema requiere una
convención propia tuya (ej. código de sede en el título de la reunión, o una tabla de
configuración `sede ↔ id de sala/organizador`), que no puedo inventar por ti sin conocer
cómo organizas tus reuniones reales. La sincronización manual (`POST /api/meetingsync/...`)
sí queda 100% funcional.

