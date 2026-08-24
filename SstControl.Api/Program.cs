using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SstControl.Api.Seguridad;
using SstControl.Aplicacion.Integraciones;
using SstControl.Aplicacion.Interfaces;
using SstControl.Infraestructura.Persistencia;
using SstControl.Infraestructura.Repositorios;
using SstControl.Infraestructura.Servicios;
using SstControl.Integraciones.Conectores;

var constructor = WebApplication.CreateBuilder(args);

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

// ---- Capa de integraciones: conectores developer-level a Teams / Google Meet / Zoom ----
constructor.Services.AddHttpClient<IConectorReunion, ConectorTeams>();
constructor.Services.AddHttpClient<IConectorReunion, ConectorZoom>();
constructor.Services.AddHttpClient<IConectorReunion, ConectorGoogleMeet>();
constructor.Services.AddScoped<IFabricaConectoresReunion, FabricaConectoresReunion>();
constructor.Services.AddScoped<IServicioSincronizacionReuniones, ServicioSincronizacionReuniones>();

// ---- Autenticación JWT (consumida por la PWA) ----
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

// ---- CORS: permite que la PWA (otro origen) consuma la API ----
constructor.Services.AddCors(opciones => opciones.AddPolicy("ClientePwa", politica =>
    politica.WithOrigins(constructor.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
     .AllowAnyHeader().AllowAnyMethod()));

var app = constructor.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ClientePwa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
