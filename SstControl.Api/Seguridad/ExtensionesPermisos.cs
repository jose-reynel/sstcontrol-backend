using System.Security.Claims;
using SstControl.Infraestructura.Servicios;

namespace SstControl.Api.Seguridad;

/// <summary>
/// Punto único donde se verifica si el usuario autenticado tiene un permiso puntual
/// (ej. "empresas.administrar"). Los permisos ya viajan como claims dentro del JWT
/// (ver ServicioAutenticacion), así que verificarlos no requiere consultar la base
/// de datos en cada petición.
/// </summary>
public static class ExtensionesPermisos
{
    /// <summary>Indica si el usuario autenticado tiene el permiso indicado.</summary>
    public static bool TienePermiso(this ClaimsPrincipal usuario, string codigoPermiso) =>
        usuario.Claims.Any(c => c.Type == ServicioAutenticacion.TipoClaimPermiso && c.Value == codigoPermiso);
}
