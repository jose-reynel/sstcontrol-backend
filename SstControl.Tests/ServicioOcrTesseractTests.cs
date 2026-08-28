using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SstControl.Infraestructura.Persistencia;
using SstControl.Infraestructura.Servicios;
using Xunit;

namespace SstControl.Tests;

/// <summary>
/// Prueba únicamente las validaciones de ServicioOcrTesseract que se resuelven
/// ANTES de invocar el motor nativo de Tesseract (tipo de archivo soportado,
/// archivo no vacío, documento existente) — el reconocimiento de texto en sí
/// requiere el binario nativo instalado (ver Dockerfile) y no es viable de
/// probar en un entorno de CI sin esa dependencia del sistema operativo.
/// </summary>
public class ServicioOcrTesseractTests
{
    private static ServicioOcrTesseract CrearServicio()
    {
        var opciones = new DbContextOptionsBuilder<ContextoBaseDatos>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var contexto = new ContextoBaseDatos(opciones);
        var configuracion = new ConfigurationBuilder().Build();
        return new ServicioOcrTesseract(contexto, configuracion);
    }

    [Fact]
    public async Task DigitalizarAsync_RechazaUnTipoDeContenidoNoSoportado()
    {
        var servicio = CrearServicio();
        using var contenido = new MemoryStream([1, 2, 3]);

        var excepcion = await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.DigitalizarAsync(1, contenido, "documento.pdf", "application/pdf"));

        Assert.Contains("no soportado", excepcion.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DigitalizarAsync_RechazaUnDocumentoQueNoExiste()
    {
        var servicio = CrearServicio();
        using var contenido = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => servicio.DigitalizarAsync(idDocumento: 999, contenido, "foto.jpg", "image/jpeg"));
    }

    [Fact]
    public async Task ObtenerDigitalizacionAsync_DevuelveNull_SiNuncaSeEscaneoElDocumento()
    {
        var servicio = CrearServicio();

        var resultado = await servicio.ObtenerDigitalizacionAsync(idDocumento: 999);

        Assert.Null(resultado);
    }
}
