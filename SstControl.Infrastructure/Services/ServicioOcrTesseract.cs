using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SstControl.Aplicacion.DTOs;
using SstControl.Aplicacion.Interfaces;
using SstControl.Dominio.Entidades;
using SstControl.Infraestructura.Persistencia;
using Tesseract;

namespace SstControl.Infraestructura.Servicios;

/// <summary>
/// Digitaliza un documento físico escaneado (foto o imagen escaneada) usando el
/// motor Tesseract OCR — 100% local, sin depender de una API de nube ni de
/// credenciales externas. Solo el resultado (texto + confianza) se persiste; la
/// imagen en sí no se guarda (ver DigitalizacionDocumento) — este servicio la
/// procesa en memoria y la descarta al terminar.
///
/// Requiere en el contenedor/host: el binario nativo tesseract-ocr y los archivos
/// de idioma entrenados (tessdata/spa.traineddata) — ver SstControl.Api/Dockerfile
/// y la clave de configuración "Ocr:RutaDatosEntrenamiento".
/// </summary>
public class ServicioOcrTesseract : IServicioOcr
{
    /// <summary>Tesseract solo procesa imágenes rasterizadas — un PDF (aunque sea un
    /// escaneo) debe convertirse a imagen antes de subirlo; convertir PDF a imagen
    /// queda fuera de este servicio a propósito, para no depender de Ghostscript u
    /// otra herramienta externa de conversión.</summary>
    private static readonly HashSet<string> TiposContenidoSoportados = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/bmp", "image/tiff",
    };

    private const long TamanioMaximoBytes = 15 * 1024 * 1024; // 15 MB

    private readonly ContextoBaseDatos _contexto;
    private readonly string _rutaDatosEntrenamiento;
    private readonly string _idioma;

    public ServicioOcrTesseract(ContextoBaseDatos contexto, IConfiguration configuracion)
    {
        _contexto = contexto;
        _rutaDatosEntrenamiento = configuracion["Ocr:RutaDatosEntrenamiento"] ?? "./tessdata";
        _idioma = configuracion["Ocr:Idioma"] ?? "spa";
    }

    public async Task<DigitalizacionDocumentoDto> DigitalizarAsync(int idDocumento, Stream contenidoArchivo, string nombreArchivo, string tipoContenido)
    {
        if (!TiposContenidoSoportados.Contains(tipoContenido))
            throw new ArgumentException(
                $"Tipo de archivo no soportado para OCR: \"{tipoContenido}\". Sube una imagen (JPEG, PNG, BMP o TIFF); " +
                "si el escaneo está en PDF, conviértelo a imagen primero.");

        var existeDocumento = await _contexto.Documentos.AnyAsync(d => d.IdDocumento == idDocumento);
        if (!existeDocumento) throw new KeyNotFoundException("Documento no encontrado");

        using var memoria = new MemoryStream();
        await contenidoArchivo.CopyToAsync(memoria);
        var bytes = memoria.ToArray();

        if (bytes.LongLength == 0) throw new ArgumentException("El archivo está vacío.");
        if (bytes.LongLength > TamanioMaximoBytes)
            throw new ArgumentException($"El archivo supera el máximo permitido de {TamanioMaximoBytes / 1024 / 1024} MB.");

        var (textoExtraido, confianza) = await Task.Run(() => ReconocerTexto(bytes));

        var digitalizacionExistente = await _contexto.DigitalizacionesDocumento.FirstOrDefaultAsync(d => d.IdDocumento == idDocumento);
        if (digitalizacionExistente is not null)
        {
            digitalizacionExistente.NombreArchivoOriginal = nombreArchivo;
            digitalizacionExistente.TipoContenido = tipoContenido;
            digitalizacionExistente.TamanioBytes = bytes.LongLength;
            digitalizacionExistente.TextoExtraido = textoExtraido;
            digitalizacionExistente.Confianza = confianza;
            digitalizacionExistente.FechaEscaneo = DateTimeOffset.UtcNow;
        }
        else
        {
            _contexto.DigitalizacionesDocumento.Add(new DigitalizacionDocumento
            {
                IdDocumento = idDocumento,
                NombreArchivoOriginal = nombreArchivo,
                TipoContenido = tipoContenido,
                TamanioBytes = bytes.LongLength,
                TextoExtraido = textoExtraido,
                Confianza = confianza,
            });
        }
        await _contexto.SaveChangesAsync();

        return await ObtenerDigitalizacionAsync(idDocumento)
            ?? throw new InvalidOperationException("No se pudo guardar la digitalización recién creada.");
    }

    public async Task<DigitalizacionDocumentoDto?> ObtenerDigitalizacionAsync(int idDocumento)
    {
        var d = await _contexto.DigitalizacionesDocumento.AsNoTracking().FirstOrDefaultAsync(x => x.IdDocumento == idDocumento);
        return d is null ? null : Mapear(d);
    }

    /// <summary>Tesseract es una librería nativa (P/Invoke), síncrona y no reentrante
    /// por instancia — se ejecuta en un hilo de trabajo dedicado (Task.Run) para no
    /// bloquear el hilo de la petición HTTP mientras procesa la imagen.</summary>
    private (string? Texto, double? Confianza) ReconocerTexto(byte[] bytesImagen)
    {
        string? texto = null;
        double? confianza = null;

        using var motor = new TesseractEngine(_rutaDatosEntrenamiento, _idioma, EngineMode.Default);
        using var imagen = Pix.LoadFromMemory(bytesImagen);
        using var pagina = motor.Process(imagen);

        texto = pagina.GetText()?.Trim();
        var confianzaBruta = pagina.GetMeanConfidence(); // 0.0 - 1.0
        confianza = confianzaBruta >= 0 ? Math.Round(confianzaBruta * 100, 1) : null;

        return (string.IsNullOrWhiteSpace(texto) ? null : texto, confianza);
    }

    private static DigitalizacionDocumentoDto Mapear(DigitalizacionDocumento d) =>
        new(d.IdDocumento, d.NombreArchivoOriginal, d.TipoContenido, d.TamanioBytes, d.TextoExtraido, d.Confianza, d.FechaEscaneo);
}
