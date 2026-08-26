using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SstControl.Api.Middleware;

/// <summary>
/// Único punto donde se atrapa cualquier excepción no controlada de toda la API.
/// Sin esto, un error de negocio (ej. "documento no encontrado") o un fallo de
/// infraestructura (ej. Postgres caído) terminaría devolviendo un HTML de error
/// genérico de ASP.NET Core — inservible para un cliente HTTP. Aquí se traduce
/// todo a application/problem+json (RFC 7807), con el detalle apropiado según
/// el tipo de excepción, y se registra en el log con su id de rastreo para
/// poder correlacionar un reporte del usuario con la traza real del servidor.
/// </summary>
public class ManejadorErroresGlobal(ILogger<ManejadorErroresGlobal> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        var idRastreo = contexto.TraceIdentifier;

        var (codigoEstado, titulo) = excepcion switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, "El recurso solicitado no existe."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "No tienes permiso para realizar esta acción."),
            ArgumentException => (StatusCodes.Status400BadRequest, "La solicitud contiene datos inválidos."),
            InvalidOperationException => (StatusCodes.Status409Conflict, "La operación no es válida en el estado actual del recurso."),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado en el servidor."),
        };

        if (codigoEstado == StatusCodes.Status500InternalServerError)
            logger.LogError(excepcion, "Error no controlado [{IdRastreo}] en {Ruta}", idRastreo, contexto.Request.Path);
        else
            logger.LogWarning(excepcion, "Error de negocio [{IdRastreo}] en {Ruta}: {Mensaje}", idRastreo, contexto.Request.Path, excepcion.Message);

        var problema = new ProblemDetails
        {
            Status = codigoEstado,
            Title = titulo,
            // El mensaje de excepción solo se expone en Development: en producción
            // podría filtrar detalles internos (nombres de tabla, rutas de archivo, etc.).
            Detail = contexto.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? excepcion.Message
                : null,
            Instance = contexto.Request.Path,
        };
        problema.Extensions["idRastreo"] = idRastreo;

        contexto.Response.StatusCode = codigoEstado;
        await contexto.Response.WriteAsJsonAsync(problema, cancelacion);
        return true;
    }
}
