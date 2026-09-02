using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Integraciones;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

public class ServicioMapeoReunion(ContextoBaseDatos contexto) : IServicioMapeoReunion
{
    public async Task<MapeoOrigenReunionDto?> ResolverAsync(ProveedorReunion proveedor, string tokenCorrelacion)
    {
        var origen = MapearOrigen(proveedor);
        var mapeo = await Consulta().FirstOrDefaultAsync(m => m.Origen == origen && m.TokenCorrelacion == tokenCorrelacion);
        return mapeo is null ? null : Proyectar(mapeo);
    }

    public async Task<MapeoOrigenReunionDto> CrearAsync(CrearMapeoOrigenReunionDto datos)
    {
        if (!Enum.TryParse<OrigenReunion>(datos.Origen, ignoreCase: true, out var origen) || origen == OrigenReunion.Manual)
            throw new ArgumentException("Origen inválido. Usa Teams, GoogleMeet, Zoom o Webex.");

        var nuevo = new MapeoOrigenReunion
        {
            Origen = origen,
            TokenCorrelacion = datos.TokenCorrelacion,
            IdEmpresa = datos.IdEmpresa,
            IdSede = datos.IdSede,
            IdUsuarioResponsable = datos.IdUsuarioResponsable,
            Descripcion = datos.Descripcion,
        };
        contexto.MapeosOrigenReunion.Add(nuevo);
        await contexto.SaveChangesAsync();

        var creado = await Consulta().FirstAsync(m => m.IdMapeo == nuevo.IdMapeo);
        return Proyectar(creado);
    }

    public async Task<IReadOnlyList<MapeoOrigenReunionDto>> ObtenerTodosAsync()
    {
        var mapeos = await Consulta().OrderBy(m => m.Empresa.Nombre).ThenBy(m => m.Sede.Nombre).ToListAsync();
        return mapeos.Select(Proyectar).ToList();
    }

    private IQueryable<MapeoOrigenReunion> Consulta() => contexto.MapeosOrigenReunion.AsNoTracking()
        .Include(m => m.Empresa).Include(m => m.Sede).Include(m => m.UsuarioResponsable);

    private static MapeoOrigenReunionDto Proyectar(MapeoOrigenReunion m) => new(
        m.IdMapeo, m.Origen.ToString(), m.TokenCorrelacion,
        m.IdEmpresa, m.Empresa.Nombre, m.IdSede, m.Sede.Nombre,
        m.IdUsuarioResponsable, m.UsuarioResponsable.NombreCompleto, m.Descripcion);

    private static OrigenReunion MapearOrigen(ProveedorReunion proveedor) => proveedor switch
    {
        ProveedorReunion.Teams => OrigenReunion.Teams,
        ProveedorReunion.GoogleMeet => OrigenReunion.GoogleMeet,
        ProveedorReunion.Zoom => OrigenReunion.Zoom,
        ProveedorReunion.Webex => OrigenReunion.Webex,
        _ => OrigenReunion.Manual,
    };
}
