using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Integraciones;
using SstControl.Aplicacion.Interfaces;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>
/// El "bot de minutas": conecta el contenido ya sincronizado de una reunión
/// (ContenidoReunion, traído por el conector de Teams/Meet/Zoom/Webex — ver
/// ServicioSincronizacionReuniones) con el seguimiento real de la Acta —
/// compromisos, responsables, fechas límite, y su vínculo con los cambios
/// documentales que los cierran.
/// </summary>
public class ServicioBotActas : IServicioBotActas
{
    private readonly ContextoBaseDatos _contexto;
    private readonly IServicioResumenReunion _servicioResumen;

    public ServicioBotActas(ContextoBaseDatos contexto, IServicioResumenReunion servicioResumen)
    {
        _contexto = contexto;
        _servicioResumen = servicioResumen;
    }

    public async Task<MinutaGeneradaDto> GenerarMinutaAsync(int idActa)
    {
        var contenido = await _contexto.ContenidosReunion.FirstOrDefaultAsync(c => c.IdActa == idActa);
        // El conector deja aquí la transcripción (si la plataforma la ofrece) o un
        // resumen textual; si solo hay un enlace de grabación (TipoContenido =
        // "recording", Resumen nulo), no hay texto que interpretar — el bot no
        // transcribe audio.
        var textoFuente = contenido?.Resumen;

        if (string.IsNullOrWhiteSpace(textoFuente))
        {
            var compromisosExistentes = await ObtenerCompromisosAsync(idActa);
            return new MinutaGeneradaDto(TextoFuente: null, Compromisos: compromisosExistentes.ToList());
        }

        var minuta = _servicioResumen.ExtraerMinuta(textoFuente);

        // Idempotencia: si ya se corrió el bot antes sobre este texto, no duplica
        // los mismos compromisos — compara contra lo que el bot mismo ya generó.
        var descripcionesExistentes = await _contexto.CompromisosActa
            .Where(c => c.IdActa == idActa && c.Origen == OrigenCompromiso.Bot)
            .Select(c => c.Descripcion)
            .ToListAsync();

        foreach (var candidato in minuta.Compromisos)
        {
            if (descripcionesExistentes.Contains(candidato.Descripcion)) continue;
            _contexto.CompromisosActa.Add(new CompromisoActa
            {
                IdActa = idActa,
                Descripcion = candidato.Descripcion,
                Responsable = candidato.Responsable,
                FechaLimite = candidato.FechaLimite,
                Origen = OrigenCompromiso.Bot,
                Estado = EstadoCompromiso.Pendiente,
            });
        }
        await _contexto.SaveChangesAsync();

        var todosLosCompromisos = await ObtenerCompromisosAsync(idActa);
        return new MinutaGeneradaDto(minuta.Resumen, todosLosCompromisos.ToList());
    }

    public async Task<IReadOnlyList<CompromisoActaDto>> ObtenerCompromisosAsync(int idActa) =>
        await _contexto.CompromisosActa.AsNoTracking()
            .Include(c => c.DocumentoRelacionado)
            .Where(c => c.IdActa == idActa)
            .OrderBy(c => c.FechaCreacion)
            .Select(c => new CompromisoActaDto(c.IdCompromiso, c.IdActa, c.Descripcion, c.Responsable, c.FechaLimite,
                c.Estado.ToString(), c.Origen.ToString(), c.IdDocumentoRelacionado,
                c.DocumentoRelacionado != null ? c.DocumentoRelacionado.Actividad : null))
            .ToListAsync();

    public async Task<CompromisoActaDto> AgregarCompromisoAsync(int idActa, CrearCompromisoDto datos)
    {
        var existeActa = await _contexto.Actas.AnyAsync(a => a.IdActa == idActa);
        if (!existeActa) throw new KeyNotFoundException("Acta no encontrada");

        var compromiso = new CompromisoActa
        {
            IdActa = idActa,
            Descripcion = datos.Descripcion,
            Responsable = datos.Responsable,
            FechaLimite = datos.FechaLimite,
            IdDocumentoRelacionado = datos.IdDocumentoRelacionado,
            Origen = OrigenCompromiso.Manual,
            Estado = EstadoCompromiso.Pendiente,
        };
        _contexto.CompromisosActa.Add(compromiso);
        await _contexto.SaveChangesAsync();
        return await MapearConDocumentoAsync(compromiso.IdCompromiso);
    }

    public async Task<CompromisoActaDto> MarcarCumplidoAsync(int idCompromiso)
    {
        var compromiso = await _contexto.CompromisosActa.FirstOrDefaultAsync(c => c.IdCompromiso == idCompromiso)
            ?? throw new KeyNotFoundException("Compromiso no encontrado");
        compromiso.Estado = EstadoCompromiso.Cumplido;
        await _contexto.SaveChangesAsync();
        return await MapearConDocumentoAsync(idCompromiso);
    }

    public async Task<CompromisoActaDto> VincularDocumentoAsync(int idCompromiso, int idDocumento)
    {
        var compromiso = await _contexto.CompromisosActa.FirstOrDefaultAsync(c => c.IdCompromiso == idCompromiso)
            ?? throw new KeyNotFoundException("Compromiso no encontrado");
        var existeDocumento = await _contexto.Documentos.AnyAsync(d => d.IdDocumento == idDocumento);
        if (!existeDocumento) throw new KeyNotFoundException("Documento no encontrado");

        compromiso.IdDocumentoRelacionado = idDocumento;
        await _contexto.SaveChangesAsync();
        return await MapearConDocumentoAsync(idCompromiso);
    }

    private async Task<CompromisoActaDto> MapearConDocumentoAsync(int idCompromiso)
    {
        var c = await _contexto.CompromisosActa.AsNoTracking().Include(x => x.DocumentoRelacionado)
            .FirstAsync(x => x.IdCompromiso == idCompromiso);
        return new CompromisoActaDto(c.IdCompromiso, c.IdActa, c.Descripcion, c.Responsable, c.FechaLimite,
            c.Estado.ToString(), c.Origen.ToString(), c.IdDocumentoRelacionado,
            c.DocumentoRelacionado != null ? c.DocumentoRelacionado.Actividad : null);
    }
}
