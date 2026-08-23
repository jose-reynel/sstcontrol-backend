using Microsoft.EntityFrameworkCore;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;

namespace SstControl.Infraestructura.Servicios;

/// <summary>
/// Implementa el ciclo (loop) documental descrito en el Documento de Diseño de Solución:
/// captura → control de tiempo → firma de aprobación → vigencia/archivo → renovación.
/// </summary>
public class ServicioDocumento : IServicioDocumento
{
    private readonly ContextoBaseDatos _contexto;
    public ServicioDocumento(ContextoBaseDatos contexto) => _contexto = contexto;

    /// <summary>Lista todos los documentos, del más reciente al más antiguo.</summary>
    public async Task<IReadOnlyList<DocumentoDto>> ObtenerTodosAsync()
    {
        return await _contexto.Documentos
            .Include(d => d.TipoDocumento)
            .Include(d => d.UsuarioAprueba)
            .OrderByDescending(d => d.IdDocumento)
            .Select(d => new DocumentoDto(d.IdDocumento, d.TipoDocumento.Nombre, d.NombreColaborador, d.Actividad,
                d.FechaCaptura, d.FechaVencimiento, d.Estado.ToString(),
                d.UsuarioAprueba != null ? d.UsuarioAprueba.NombreCompleto : null))
            .ToListAsync();
    }

    /// <summary>Registra un nuevo documento — queda como "pendiente" hasta que se firme.</summary>
    public async Task<DocumentoDto> CrearAsync(CrearDocumentoDto datos)
    {
        var entidad = new Documento
        {
            IdTipoDocumento = datos.IdTipoDocumento,
            NombreColaborador = datos.NombreColaborador,
            Actividad = datos.Actividad,
            FechaCaptura = DateOnly.FromDateTime(DateTime.UtcNow),
            FechaVencimiento = datos.FechaVencimiento,
            IdEmpresa = datos.IdEmpresa,
            IdSede = datos.IdSede,
            Estado = EstadoDocumento.Pendiente,
        };
        _contexto.Documentos.Add(entidad);
        await _contexto.SaveChangesAsync();
        return await MapearAsync(entidad.IdDocumento);
    }

    /// <summary>Aprueba el documento y registra quién lo firmó y cuándo.</summary>
    public async Task<DocumentoDto> FirmarAsync(int idDocumento, int idUsuarioAprueba)
    {
        var documento = await _contexto.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == idDocumento)
            ?? throw new KeyNotFoundException("Documento no encontrado");
        documento.Estado = EstadoDocumento.Aprobado;
        documento.IdUsuarioAprueba = idUsuarioAprueba;
        documento.FechaFirma = DateTimeOffset.UtcNow;
        await _contexto.SaveChangesAsync();
        return await MapearAsync(idDocumento);
    }

    /// <summary>Cierra el ciclo: crea un nuevo documento pendiente a partir de uno
    /// vencido o por vencer, con una nueva fecha de vencimiento a 30 días.</summary>
    public async Task<DocumentoDto> RenovarAsync(int idDocumento)
    {
        var original = await _contexto.Documentos.FirstOrDefaultAsync(d => d.IdDocumento == idDocumento)
            ?? throw new KeyNotFoundException("Documento no encontrado");
        var renovado = new Documento
        {
            IdTipoDocumento = original.IdTipoDocumento,
            NombreColaborador = original.NombreColaborador,
            Actividad = original.Actividad,
            IdEmpresa = original.IdEmpresa,
            IdSede = original.IdSede,
            FechaCaptura = DateOnly.FromDateTime(DateTime.UtcNow),
            FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Estado = EstadoDocumento.Pendiente,
        };
        _contexto.Documentos.Add(renovado);
        await _contexto.SaveChangesAsync();
        return await MapearAsync(renovado.IdDocumento);
    }

    public async Task EliminarAsync(int idDocumento)
    {
        var documento = await _contexto.Documentos.FindAsync(idDocumento);
        if (documento is null) return;
        _contexto.Documentos.Remove(documento);
        await _contexto.SaveChangesAsync();
    }

    /// <summary>Convierte la entidad de dominio en el DTO expuesto por la API.</summary>
    private async Task<DocumentoDto> MapearAsync(int idDocumento)
    {
        var d = await _contexto.Documentos.Include(x => x.TipoDocumento).Include(x => x.UsuarioAprueba)
            .FirstAsync(x => x.IdDocumento == idDocumento);
        return new DocumentoDto(d.IdDocumento, d.TipoDocumento.Nombre, d.NombreColaborador, d.Actividad,
            d.FechaCaptura, d.FechaVencimiento, d.Estado.ToString(), d.UsuarioAprueba?.NombreCompleto);
    }
}
