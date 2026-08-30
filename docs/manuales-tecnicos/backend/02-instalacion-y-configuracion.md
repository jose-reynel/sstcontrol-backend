# Manual técnico (backend) — Instalación y configuración

## Requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS).
- PostgreSQL 16 (local, en contenedor, o gestionado).
- Para OCR (`documentos.escanear`): el binario nativo `tesseract-ocr` +
  datos de idioma (`tesseract-ocr-spa` en Debian/Ubuntu) — ya resuelto en el
  `Dockerfile`; en desarrollo local sin Docker instálalos con tu gestor de
  paquetes del sistema operativo.

## 1. Levantar PostgreSQL
Con Docker (más rápido para desarrollo):
```bash
docker run -d --name sst-postgres -e POSTGRES_DB=sst_control \
  -e POSTGRES_USER=sst_user -e POSTGRES_PASSWORD=CAMBIA_ESTA_CLAVE \
  -p 5432:5432 postgres:16
```
O usa `docker compose up` (ver `03-despliegue-en-servidor-de-aplicaciones.md`).

## 2. Crear la primera migración y aplicarla
El repositorio **todavía no incluye ninguna migración generada** — es lo
primero que debes hacer:
```bash
cd SstControl.Api
dotnet tool install --global dotnet-ef   # si no lo tienes
dotnet ef migrations add InicialCompleta --project ../SstControl.Infrastructure --startup-project .
dotnet ef database update --project ../SstControl.Infrastructure --startup-project .
```
Esto crea todas las tablas descritas en `01-arquitectura-y-modelo-de-datos.md`
(incluidas las de Capacitación/Calidad, aunque no tengan API todavía — el
modelo de datos ya las contempla).

## 3. Configurar `appsettings.json` / `appsettings.Development.json`
Copia `SstControl.Api/appsettings.Development.json.example` a
`SstControl.Api/appsettings.Development.json` (git lo ignora — nunca subas
credenciales reales) y ajusta cada bloque:

| Clave | Para qué sirve |
|---|---|
| `ConnectionStrings:Default` | Cadena de conexión a PostgreSQL. |
| `Jwt:Key` | Firma de los tokens — mínimo 32 caracteres aleatorios (`openssl rand -base64 48`). **Nunca la reutilices entre ambientes.** |
| `Jwt:MinutosExpiracion` | Duración del JWT antes de necesitar renovarse (480 = 8 h por defecto). |
| `Jwt:DiasVigenciaTokenRenovacion` | Duración de la sesión completa antes de pedir la contraseña de nuevo (30 días por defecto). |
| `Ocr:RutaDatosEntrenamiento` / `Ocr:Idioma` | Dónde están los datos de idioma de Tesseract, y en qué idioma reconocer texto (`spa`). |
| `Cors:AllowedOrigins` | Lista de orígenes permitidos a llamar la API — agrega ahí la URL del frontend Web y, en desarrollo, el puerto del servidor local de Blazor. |
| `Integraciones:Teams` / `Zoom` / `GoogleMeet` / `Webex` | Credenciales de cada plataforma de videollamada — ver `04-seguridad-rbac-e-integraciones.md` para cómo obtener cada una. |

## 4. Ejecutar la API
```bash
dotnet run --project SstControl.Api
```
Swagger disponible en `https://localhost:xxxx/swagger`. Estado de salud
(usado por `docker-compose.yml` y por cualquier orquestador) en `GET /salud`.

## 5. Correr los datos semilla
Con la base de datos ya migrada, ejecuta en orden los scripts de
`docs/datos-semilla/` — ver `docs/datos-semilla/LEEME.md` para el orden
exacto y los prerrequisitos.

## 6. Verificar que todo funciona
```bash
dotnet build SstControl.sln -c Release
dotnet test SstControl.sln -c Release
```
Si algo falla al restaurar el paquete `Tesseract` (usado por
`ServicioOcrTesseract`), verifica que la versión fijada en
`SstControl.Infrastructure.csproj` siga existiendo en NuGet — no se pudo
verificar contra el feed real al escribir este manual.
