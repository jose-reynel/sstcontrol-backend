using Microsoft.Extensions.Diagnostics.HealthChecks;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Api.Salud;

/// <summary>
/// Comprueba conectividad real contra Postgres (no solo "el proceso está vivo").
/// Se implementa a mano en vez de con el paquete AspNetCore.HealthChecks.NpgSql
/// para no depender de una versión de paquete de terceros que no se puede
/// verificar sin acceso a NuGet en este momento — Database.CanConnectAsync()
/// ya viene con Microsoft.EntityFrameworkCore, que el proyecto ya referencia.
/// Usado por /salud (ver Program.cs) — es lo que docker-compose y cualquier
/// orquestador (Kubernetes, ECS, etc.) deberían consultar para saber si el
/// contenedor está realmente listo para recibir tráfico.
/// </summary>
public class VerificacionBaseDatos(ContextoBaseDatos contexto) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext contextoVerificacion, CancellationToken cancelacion = default)
    {
        try
        {
            return await contexto.Database.CanConnectAsync(cancelacion)
                ? HealthCheckResult.Healthy("Conexión a PostgreSQL correcta.")
                : HealthCheckResult.Unhealthy("No se pudo conectar a PostgreSQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Excepción al verificar PostgreSQL.", ex);
        }
    }
}
