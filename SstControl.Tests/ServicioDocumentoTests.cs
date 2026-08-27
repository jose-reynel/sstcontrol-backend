using Microsoft.EntityFrameworkCore;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;
using SstControl.Infraestructura.Servicios;
using Xunit;

namespace SstControl.Tests;

/// <summary>
/// Prueba la paginación y el resumen agregado de ServicioDocumento contra un
/// proveedor EF Core en memoria — verifica la lógica de consulta real (Skip/Take,
/// Count, filtros por estado y fecha), no solo la forma de los DTOs.
/// </summary>
public class ServicioDocumentoTests
{
    private static ContextoBaseDatos CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoBaseDatos>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ContextoBaseDatos(opciones);
    }

    private static async Task<ContextoBaseDatos> SembrarDocumentosAsync(int cantidad)
    {
        var contexto = CrearContexto();
        var tipo = new TipoDocumento { Nombre = "Charla de 5 minutos" };
        contexto.TiposDocumento.Add(tipo);
        await contexto.SaveChangesAsync();

        for (var i = 0; i < cantidad; i++)
        {
            contexto.Documentos.Add(new Documento
            {
                IdTipoDocumento = tipo.IdTipoDocumento,
                NombreColaborador = $"Colaborador {i}",
                Actividad = "Actividad de prueba",
                FechaCaptura = DateOnly.FromDateTime(DateTime.UtcNow),
                FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Estado = EstadoDocumento.Pendiente,
            });
        }
        await contexto.SaveChangesAsync();
        return contexto;
    }

    [Fact]
    public async Task ObtenerPaginadoAsync_DevuelveElTotalCorrecto_AunqueLaPaginaTraigaMenos()
    {
        var contexto = await SembrarDocumentosAsync(25);
        var servicio = new ServicioDocumento(contexto);

        var pagina = await servicio.ObtenerPaginadoAsync(pagina: 1, tamanioPagina: 10);

        Assert.Equal(10, pagina.Elementos.Count);
        Assert.Equal(25, pagina.TotalElementos);
    }

    [Fact]
    public async Task ObtenerPaginadoAsync_LaSegundaPagina_NoRepiteElementosDeLaPrimera()
    {
        var contexto = await SembrarDocumentosAsync(25);
        var servicio = new ServicioDocumento(contexto);

        var primera = await servicio.ObtenerPaginadoAsync(1, 10);
        var segunda = await servicio.ObtenerPaginadoAsync(2, 10);

        Assert.Equal(10, segunda.Elementos.Count);
        var idsRepetidos = primera.Elementos.Select(d => d.IdDocumento).Intersect(segunda.Elementos.Select(d => d.IdDocumento));
        Assert.Empty(idsRepetidos);
    }

    [Fact]
    public async Task ObtenerPaginadoAsync_LimitaElTamanioDePaginaA100()
    {
        var contexto = await SembrarDocumentosAsync(150);
        var servicio = new ServicioDocumento(contexto);

        var pagina = await servicio.ObtenerPaginadoAsync(pagina: 1, tamanioPagina: 500);

        Assert.Equal(100, pagina.Elementos.Count);
        Assert.Equal(100, pagina.TamanioPagina);
    }

    [Fact]
    public async Task ObtenerPaginadoAsync_UnaPaginaMenorA1_SeTrataComoLaPrimera()
    {
        var contexto = await SembrarDocumentosAsync(5);
        var servicio = new ServicioDocumento(contexto);

        var pagina = await servicio.ObtenerPaginadoAsync(pagina: 0, tamanioPagina: 10);

        Assert.Equal(1, pagina.Pagina);
        Assert.Equal(5, pagina.Elementos.Count);
    }

    [Fact]
    public async Task ObtenerResumenAsync_ClasificaPendientesVencidosYAprobadosPorSeparado()
    {
        var contexto = CrearContexto();
        var tipo = new TipoDocumento { Nombre = "Prueba" };
        contexto.TiposDocumento.Add(tipo);
        await contexto.SaveChangesAsync();

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        contexto.Documentos.AddRange(
            new Documento
            {
                IdTipoDocumento = tipo.IdTipoDocumento, NombreColaborador = "A", Actividad = "x",
                FechaCaptura = hoy, FechaVencimiento = hoy.AddDays(-1), Estado = EstadoDocumento.Pendiente,
            },
            new Documento
            {
                IdTipoDocumento = tipo.IdTipoDocumento, NombreColaborador = "B", Actividad = "x",
                FechaCaptura = hoy, FechaVencimiento = hoy.AddDays(10), Estado = EstadoDocumento.Pendiente,
            },
            new Documento
            {
                IdTipoDocumento = tipo.IdTipoDocumento, NombreColaborador = "C", Actividad = "x",
                FechaCaptura = hoy, FechaVencimiento = hoy.AddDays(10), Estado = EstadoDocumento.Aprobado,
            });
        await contexto.SaveChangesAsync();

        var servicio = new ServicioDocumento(contexto);
        var resumen = await servicio.ObtenerResumenAsync();

        Assert.Equal(3, resumen.Total);
        Assert.Equal(1, resumen.Vencidos);
        Assert.Equal(1, resumen.Pendientes);
        Assert.Equal(1, resumen.Aprobados);
    }
}
