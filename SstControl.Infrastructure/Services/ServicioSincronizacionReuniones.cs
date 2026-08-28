using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.Integraciones;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>
/// Cierra el ciclo de interoperabilidad: trae la reunión + asistentes + contenido desde
/// Teams/Google Meet/Zoom y los persiste como una Acta real en el sistema central, con
/// trazabilidad a nivel de cada asistente (AsistenteReunion), no solo texto libre.
/// </summary>
public class ServicioSincronizacionReuniones : IServicioSincronizacionReuniones
{
    private readonly IFabricaConectoresReunion _fabricaConectores;
    private readonly ContextoBaseDatos _contexto;

    public ServicioSincronizacionReuniones(IFabricaConectoresReunion fabricaConectores, ContextoBaseDatos contexto)
    {
        _fabricaConectores = fabricaConectores;
        _contexto = contexto;
    }

    /// <summary>Sincroniza una reunión externa; es idempotente — si ya se había
    /// sincronizado antes, actualiza el acta existente en vez de duplicarla.</summary>
    public async Task<int> SincronizarReunionAsync(ProveedorReunion proveedor, string idReunionExterna, int idEmpresa,
        int idSede, TipoActa tipo, int idUsuarioCreador)
    {
        var conector = _fabricaConectores.Resolver(proveedor);

        var reunion = await conector.ObtenerReunionAsync(idReunionExterna);
        var asistencia = await conector.ObtenerAsistenciaAsync(idReunionExterna);
        var contenido = await conector.ObtenerContenidoAsync(idReunionExterna);

        var origen = MapearOrigen(proveedor);
        var actaExistente = await _contexto.Actas.FirstOrDefaultAsync(a =>
            a.IdReunionExterna == idReunionExterna && a.Origen == origen);

        var acta = actaExistente ?? new Acta
        {
            IdEmpresa = idEmpresa,
            IdSede = idSede,
            Tipo = tipo,
            IdUsuarioCreador = idUsuarioCreador,
            Origen = origen,
            IdReunionExterna = idReunionExterna,
        };

        acta.Titulo = reunion.Titulo;
        acta.Fecha = DateOnly.FromDateTime(reunion.FechaInicio.UtcDateTime);
        acta.UrlIngresoExterna = reunion.UrlIngreso;
        acta.FechaSincronizacion = DateTimeOffset.UtcNow;
        acta.Asistentes = string.Join(", ", asistencia.Select(a => a.Nombre)); // compatibilidad con el resumen libre

        if (actaExistente is null) _contexto.Actas.Add(acta);
        else _contexto.AsistentesReunion.RemoveRange(_contexto.AsistentesReunion.Where(a => a.IdActa == acta.IdActa));

        await _contexto.SaveChangesAsync(); // asegura IdActa antes de insertar los hijos

        foreach (var asistente in asistencia)
        {
            _contexto.AsistentesReunion.Add(new AsistenteReunion
            {
                IdActa = acta.IdActa,
                Nombre = asistente.Nombre,
                CorreoElectronico = asistente.CorreoElectronico,
                HoraIngreso = asistente.HoraIngreso,
                HoraSalida = asistente.HoraSalida,
                DuracionMinutos = (asistente.HoraIngreso.HasValue && asistente.HoraSalida.HasValue)
                    ? (int)(asistente.HoraSalida.Value - asistente.HoraIngreso.Value).TotalMinutes : null,
            });
        }

        if (contenido != null)
        {
            var contenidoExistente = await _contexto.ContenidosReunion.FindAsync(acta.IdActa);
            if (contenidoExistente != null) _contexto.ContenidosReunion.Remove(contenidoExistente);
            _contexto.ContenidosReunion.Add(new ContenidoReunion
            {
                IdActa = acta.IdActa,
                Resumen = contenido.Resumen,
                UrlGrabacion = contenido.UrlGrabacion,
                TipoContenido = contenido.TipoContenido,
            });
        }

        _contexto.RegistrosAuditoria.Add(new RegistroAuditoria
        {
            IdUsuario = idUsuarioCreador,
            Accion = "Reunión sincronizada",
            Detalle = $"{proveedor} — {reunion.Titulo} ({asistencia.Count} asistentes)",
        });

        await _contexto.SaveChangesAsync();
        return acta.IdActa;
    }

    private static OrigenReunion MapearOrigen(ProveedorReunion proveedor) => proveedor switch
    {
        ProveedorReunion.Teams => OrigenReunion.Teams,
        ProveedorReunion.GoogleMeet => OrigenReunion.GoogleMeet,
        ProveedorReunion.Zoom => OrigenReunion.Zoom,
        ProveedorReunion.Webex => OrigenReunion.Webex,
        _ => OrigenReunion.Manual,
    };
}
