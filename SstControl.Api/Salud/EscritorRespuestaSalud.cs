using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SstControl.Api.Salud;

/// <summary>
/// Formato de respuesta por defecto de los health checks de ASP.NET Core es
/// solo texto plano ("Healthy"/"Unhealthy"). Este escritor lo reemplaza por
/// JSON con el detalle de cada verificación individual — lo que espera
/// cualquier orquestador o dashboard de monitoreo real (y lo que un humano
/// necesita para saber *qué* está fallando, no solo *que* algo falla).
/// </summary>
public static class EscritorRespuestaSalud
{
    public static Task EscribirAsync(HttpContext contexto, HealthReport reporte)
    {
        contexto.Response.ContentType = "application/json";

        var resultado = new
        {
            estado = reporte.Status.ToString(),
            duracionTotalMs = reporte.TotalDuration.TotalMilliseconds,
            verificaciones = reporte.Entries.Select(entrada => new
            {
                nombre = entrada.Key,
                estado = entrada.Value.Status.ToString(),
                descripcion = entrada.Value.Description,
                duracionMs = entrada.Value.Duration.TotalMilliseconds,
            }),
        };

        return contexto.Response.WriteAsync(JsonSerializer.Serialize(resultado));
    }
}
