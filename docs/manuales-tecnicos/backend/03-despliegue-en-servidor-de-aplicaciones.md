# Manual técnico (backend) — Despliegue en un servidor de aplicaciones

Dos caminos: **contenedor Docker** (recomendado, es lo que ya trae el repo
listo para usar) o **IIS/servicio nativo de Windows** (si tu servidor no
puede correr contenedores). Ambos terminan en el mismo binario: la
publicación de `SstControl.Api`.

## Opción A — Contenedor Docker (recomendada)

### 1. Variables de entorno
```bash
cp .env.example .env
# Edita .env: POSTGRES_PASSWORD, JWT_KEY (openssl rand -base64 48),
# CORS_ORIGEN_PRINCIPAL con el dominio real donde publiques el frontend Web.
```
`docker-compose.yml` **falla explícitamente** si `.env` no está completo —
no arranca con una contraseña o llave por defecto, a propósito.

### 2. Levantar el stack completo
```bash
docker compose up -d --build
```
Esto levanta PostgreSQL (con healthcheck `pg_isready`) y la API (espera a
que Postgres esté realmente listo — `depends_on: condition: service_healthy`
— antes de arrancar, evitando el clásico error de conexión rechazada en el
primer intento). La API queda expuesta en el puerto `5080` del host
(`ports: "5080:8080"`).

### 3. Aplicar la migración y los datos semilla dentro del contenedor
```bash
# Migración (una sola vez, o cada vez que el modelo de datos cambie):
docker compose exec api dotnet ef database update \
  --project /src/SstControl.Infrastructure --startup-project /src/SstControl.Api

# Datos semilla (ver docs/datos-semilla/LEEME.md para el orden):
docker compose exec -T postgres psql -U sst_user -d sst_control < docs/datos-semilla/01_catalogos_rbac.sql
docker compose exec -T postgres psql -U sst_user -d sst_control < docs/datos-semilla/02_empresas_y_sedes.sql
docker compose exec -T postgres psql -U sst_user -d sst_control < docs/datos-semilla/03_usuarios_y_asignacion_roles.sql
docker compose exec -T postgres psql -U sst_user -d sst_control < docs/datos-semilla/04_transacciones_simuladas.sql
```

### 4. Verificar
```bash
curl http://localhost:5080/salud
```
Debe responder `{"estado":"Healthy", ...}`. Si tu servidor de aplicaciones
está detrás de un proxy inverso (Nginx, Apache, un balanceador), apúntalo a
este puerto y termina TLS ahí — la API en sí corre en HTTP dentro del
contenedor (`EXPOSE 8080`).

### 5. Actualizar a una versión nueva
```bash
git pull
docker compose up -d --build   # reconstruye solo lo que cambió
docker compose exec api dotnet ef database update \
  --project /src/SstControl.Infrastructure --startup-project /src/SstControl.Api
```

## Opción B — IIS / servicio nativo (sin contenedores)

### 1. Publicar
```bash
dotnet publish SstControl.Api -c Release -o C:\sitios\sstcontrol-api
```

### 2. Requisitos del servidor Windows
- [Hosting Bundle de ASP.NET Core 10](https://dotnet.microsoft.com/download/dotnet/10.0)
  instalado (incluye el módulo `AspNetCoreModuleV2` para IIS).
- Motor **Tesseract OCR** instalado en el servidor (necesario para
  `documentos.escanear`) — en Windows, instala el paquete oficial de
  [Tesseract para Windows](https://github.com/UB-Mannheim/tesseract) e
  incluye el idioma español; ajusta `Ocr:RutaDatosEntrenamiento` en el
  `appsettings.json` publicado apuntando a la carpeta `tessdata` real.
- PostgreSQL accesible desde el servidor (local o remoto).

### 3. Configurar el sitio en IIS
1. Crea un **Application Pool** con "No Managed Code" (.NET Core corre
   fuera del CLR de IIS).
2. Crea el sitio apuntando a `C:\sitios\sstcontrol-api`.
3. Copia tu `appsettings.Production.json` con las credenciales reales junto
   al binario publicado (nunca lo subas al repo — ver
   `02-instalacion-y-configuracion.md`).
4. Aplica la migración desde una máquina con acceso a la base de datos:
   ```bash
   dotnet ef database update --project SstControl.Infrastructure --startup-project SstControl.Api
   ```
5. Corre los scripts de `docs/datos-semilla/` con tu cliente de PostgreSQL
   habitual (pgAdmin, `psql`, DBeaver...).

### 4. TLS y dominio
Configura el binding HTTPS del sitio en IIS con tu certificado real, y
agrega ese dominio a `Cors:AllowedOrigins` en el `appsettings.Production.json`
del servidor — sin eso, el frontend Web quedará bloqueado por CORS aunque la
API esté arriba y funcionando.

## CI (referencia)
`.github/workflows/ci.yml` ya compila, prueba y valida que la imagen Docker
construye en cada push — úsalo como base si quieres automatizar el
despliegue (agregar un paso de `docker compose up` contra tu servidor real
queda fuera del alcance de este repo, ya que depende de cómo accedas a tu
servidor: SSH, un runner autoalojado, etc.).
