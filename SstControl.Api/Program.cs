using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SstControl.Api.Middleware;
using SstControl.Api.Salud;
using SstControl.Api.Seguridad;
using SstControl.Aplicacion.Integraciones;
using SstControl.Aplicacion.Interfaces;
using SstControl.Infraestructura.Persistencia;
using SstControl.Infraestructura.Repositorios;
using SstControl.Infraestructura.Servicios;
using SstControl.Integraciones.Conectores;

var constructor = WebApplication.CreateBuilder(args);

// ---- Logging estructurado (Serilog): reemplaza el logger por defecto de
// ASP.NET Core por uno que escribe JSON a consola, enriquecido con el id de
// rastreo de cada request — indispensable para depurar en producción y para
// conectar cualquier stack de observabilidad (Seq, Grafana Loki, ELK...). ----
constructor.Host.UseSerilog((contexto, configuracionLog) => configuracionLog
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Aplicacion", "SstControl.Api")
    .WriteTo.Console());

// ---- Persistencia: EF Core + Npgsql (PostgreSQL) ----
constructor.Services.AddDbContext<ContextoBaseDatos>(opciones =>
    opciones.UseNpgsql(constructor.Configuration.GetConnectionString("Default")));

// ---- Capa de repositorios y servicios de negocio ----
constructor.Services.AddScoped(typeof(IRepositorio<>), typeof(RepositorioEf<>));
constructor.Services.AddScoped<IServicioDocumento, ServicioDocumento>();
constructor.Services.AddScoped<IServicioEmpresa, ServicioEmpresa>();
constructor.Services.AddScoped<IServicioActa, ServicioActa>();
constructor.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
constructor.Services.AddScoped<IServicioControlAcceso, ServicioControlAcceso>();

// ---- Bot de minutas: seguimiento de compromisos de una Acta ----
// La implementación heurística no requiere configuración; si más adelante se
// conecta un proveedor de IA real, basta con registrar otra IServicioResumenReunion aquí.
constructor.Services.AddScoped<IServicioResumenReunion, ServicioResumenReunionHeuristico>();
constructor.Services.AddScoped<IServicioBotActas, ServicioBotActas>();

// ---- Digitalización (OCR) de documentos físicos escaneados ----
constructor.Services.AddScoped<IServicioOcr, ServicioOcrTesseract>();

// ---- Capa de integraciones: conectores developer-level a Teams / Google Meet / Zoom / Webex ----
constructor.Services.AddHttpClient<IConectorReunion, ConectorTeams>();
constructor.Services.AddHttpClient<IConectorReunion, ConectorZoom>();
constructor.Services.AddHttpClient<IConectorReunion, ConectorGoogleMeet>();
constructor.Services.AddHttpClient<IConectorReunion, ConectorWebex>();
constructor.Services.AddScoped<IFabricaConectoresReunion, FabricaConectoresReunion>();
constructor.Services.AddScoped<IServicioSincronizacionReuniones, ServicioSincronizacionReuniones>();
constructor.Services.AddScoped<IServicioMapeoReunion, ServicioMapeoReunion>();

// ---- Autenticación JWT (consumida por la PWA y la app Maui) ----
constructor.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = constructor.Configuration["Jwt:Issuer"],
            ValidAudience = constructor.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(constructor.Configuration["Jwt:Key"]!)),
            // Sin margen de tolerancia de reloj: un token vencido se rechaza de
            // inmediato en vez de aceptarse unos minutos de más por defecto.
            ClockSkew = TimeSpan.Zero,
        };
    });

// ---- Autorización por permiso (RBAC): cualquier [Authorize(Policy = "modulo.accion")]
// se resuelve dinámicamente contra los claims "permiso" del JWT — ver
// SstControl.Api.Seguridad.ProveedorPoliticasPermiso y ManejadorPermiso. ----
constructor.Services.AddSingleton<IAuthorizationPolicyProvider, ProveedorPoliticasPermiso>();
constructor.Services.AddSingleton<IAuthorizationHandler, ManejadorPermiso>();
constructor.Services.AddAuthorization();

constructor.Services.AddControllers();
constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();

// ---- Manejo global de errores: toda excepción no controlada se traduce a
// application/problem+json (RFC 7807) en vez de a un HTML genérico. ----
constructor.Services.AddExceptionHandler<ManejadorErroresGlobal>();
constructor.Services.AddProblemDetails();

// ---- Health checks: expuestos en /salud para que docker-compose, Kubernetes
// o cualquier balanceador sepan si el contenedor está realmente listo. ----
constructor.Services.AddHealthChecks()
    .AddCheck<VerificacionBaseDatos>("base-de-datos");

// ---- Límite de tasa: protege el login de ataques de fuerza bruta con una
// ventana estricta por IP, y aplica un límite general más laxo al resto de la
// API para evitar que un cliente descontrolado (o un abuso deliberado)
// degrade el servicio para todos los demás. ----
constructor.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opciones.AddFixedWindowLimiter("inicio-sesion", o =>
    {
        o.PermitLimit = 5;
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    });

    opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20,
            }));
});

// ---- CORS: acepta una lista de orígenes desde configuración (Cors:AllowedOrigins,
// un array) — así se puede permitir a la vez el servidor de desarrollo local
// (Web y Maui) y el dominio de producción (GitHub Pages) sin recompilar. Se
// mantiene compatibilidad con la clave anterior Cors:AllowedOrigin (string). ----
var origenesPermitidos = constructor.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { constructor.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173" };

constructor.Services.AddCors(opciones => opciones.AddPolicy("ClientePwa", politica =>
    politica.WithOrigins(origenesPermitidos).AllowAnyHeader().AllowAnyMethod()));

var app = constructor.Build();

// Serilog: registra cada request HTTP (método, ruta, código, duración) en una
// sola línea estructurada — reemplaza el logging de request por defecto.
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("ClientePwa");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/salud", new HealthCheckOptions
{
    ResponseWriter = EscritorRespuestaSalud.EscribirAsync,
});

app.Run();
