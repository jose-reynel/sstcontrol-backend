using System.Text.RegularExpressions;
using SstControl.Aplicacion.Integraciones;

namespace SstControl.Infraestructura.Servicios;

/// <summary>
/// Implementación heurística (basada en reglas de texto, NO en un modelo de
/// lenguaje) de IServicioResumenReunion: detecta líneas que parecen compromisos
/// o acuerdos por palabras clave típicas de una minuta ("acción:", "compromiso:",
/// "pendiente:", "responsable:", casillas "- [ ]"), y les extrae responsable/fecha
/// límite si el texto los menciona en un formato reconocible.
///
/// Esto es intencionalmente simple: es un primer barrido rápido de revisar y
/// corregir a mano — no "entiende" la reunión ni genera un resumen real. Existe
/// como implementación por defecto, gratuita y sin dependencias externas, del
/// puerto IServicioResumenReunion; conectar un proveedor de IA real (ej. la API de
/// Anthropic) más adelante es cuestión de escribir otra implementación de la misma
/// interfaz y cambiar un registro en Program.cs — nada más se ve afectado.
/// </summary>
public class ServicioResumenReunionHeuristico : IServicioResumenReunion
{
    private static readonly string[] PalabrasClaveCompromiso =
    [
        "acción:", "accion:", "compromiso:", "pendiente:", "tarea:", "acuerdo:",
        "seguimiento:", "se compromete", "queda pendiente", "por hacer:", "to-do:", "todo:",
    ];

    private static readonly Regex PatronResponsable = new(@"responsable:\s*([^\.,;\n]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PatronFecha = new(@"\b(\d{1,2})[/\-](\d{1,2})[/\-](\d{2,4})\b", RegexOptions.Compiled);
    private static readonly Regex PatronCasilla = new(@"^\[[ xX]\]\s*", RegexOptions.Compiled);
    private const int LargoResumenPreliminar = 400;

    public MinutaExtraidaDto ExtraerMinuta(string textoFuente)
    {
        if (string.IsNullOrWhiteSpace(textoFuente))
            return new MinutaExtraidaDto(Resumen: null, Compromisos: []);

        var resumen = textoFuente.Length > LargoResumenPreliminar
            ? textoFuente[..LargoResumenPreliminar].TrimEnd() + "…"
            : textoFuente.Trim();

        var compromisos = new List<CompromisoExtraidoDto>();
        var lineas = textoFuente.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var linea in lineas)
        {
            if (!EsLineaCandidata(linea)) continue;

            var descripcion = LimpiarDescripcion(linea);
            if (string.IsNullOrWhiteSpace(descripcion)) continue;

            string? responsable = null;
            var coincidenciaResponsable = PatronResponsable.Match(linea);
            if (coincidenciaResponsable.Success) responsable = coincidenciaResponsable.Groups[1].Value.Trim();

            DateOnly? fechaLimite = null;
            var coincidenciaFecha = PatronFecha.Match(linea);
            if (coincidenciaFecha.Success && TryParseFecha(coincidenciaFecha, out var fecha)) fechaLimite = fecha;

            compromisos.Add(new CompromisoExtraidoDto(descripcion, responsable, fechaLimite));
        }

        return new MinutaExtraidaDto(resumen, compromisos);
    }

    private static bool EsLineaCandidata(string linea)
    {
        var lineaMinuscula = linea.ToLowerInvariant();
        var lineaSinEspacios = linea.TrimStart();
        return PalabrasClaveCompromiso.Any(palabra => lineaMinuscula.Contains(palabra))
            || lineaSinEspacios.StartsWith("- [ ]") || lineaSinEspacios.StartsWith("* [ ]")
            || lineaSinEspacios.StartsWith("- [x]", StringComparison.OrdinalIgnoreCase)
            || lineaSinEspacios.StartsWith("* [x]", StringComparison.OrdinalIgnoreCase);
    }

    private static string LimpiarDescripcion(string linea)
    {
        var limpia = linea.Trim().TrimStart('-', '*', ' ');
        limpia = PatronCasilla.Replace(limpia, "");
        foreach (var palabra in PalabrasClaveCompromiso)
            limpia = Regex.Replace(limpia, Regex.Escape(palabra), "", RegexOptions.IgnoreCase);
        return limpia.Trim(' ', ':', '-');
    }

    private static bool TryParseFecha(Match coincidencia, out DateOnly fecha)
    {
        fecha = default;
        var dia = int.Parse(coincidencia.Groups[1].Value);
        var mes = int.Parse(coincidencia.Groups[2].Value);
        var textoAnio = coincidencia.Groups[3].Value;
        var anio = int.Parse(textoAnio.Length == 2 ? "20" + textoAnio : textoAnio);
        try { fecha = new DateOnly(anio, mes, dia); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}
