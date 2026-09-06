using System.Globalization;

namespace Cotizador3D.Core.Calculation;

/// <summary>
/// Reproduce la semantica de <c>get_float_from_entry</c> del legado
/// (docs/SPEC-legacy.md 2.1): texto vacio -> valor por defecto, <c>trim</c>,
/// coma -> punto, parseo con cultura invariante. No admite separadores de
/// miles, igual que el legado.
/// </summary>
public static class NumberParser
{
    /// <summary>
    /// Intenta parsear el texto. Devuelve true si el texto era vacio (usa el
    /// valor por defecto) o si el numero es valido.
    /// </summary>
    public static bool TryParse(string? texto, out double valor, double porDefecto = 0)
    {
        if (string.IsNullOrEmpty(texto))
        {
            valor = porDefecto;
            return true;
        }

        var normalizado = texto.Trim().Replace(',', '.');
        if (double.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out var parseado)
            && !double.IsNaN(parseado)
            && !double.IsInfinity(parseado))
        {
            valor = parseado;
            return true;
        }

        valor = porDefecto;
        return false;
    }

    /// <summary>Parsea el texto o lanza <see cref="QuoteValidationException"/>.</summary>
    public static double Parse(string? texto, double porDefecto = 0, string? campo = null)
    {
        if (TryParse(texto, out var valor, porDefecto))
        {
            return valor;
        }

        var etiqueta = string.IsNullOrWhiteSpace(campo) ? string.Empty : $" en \"{campo}\"";
        throw new QuoteValidationException(
            $"El valor \"{texto}\"{etiqueta} no es un número válido. Use punto o coma como separador decimal, sin separador de miles.",
            campo);
    }

    /// <summary>
    /// Parsea sin lanzar: si el texto no es valido devuelve el valor por
    /// defecto. Se usa al leer el JSON (carga tolerante).
    /// </summary>
    public static double ParseOrDefault(string? texto, double porDefecto = 0)
    {
        TryParse(texto, out var valor, porDefecto);
        return valor;
    }

    /// <summary>
    /// Serializa un numero como lo espera el JSON legado: cultura invariante,
    /// punto decimal, sin separador de miles ni notacion rara.
    /// </summary>
    public static string ToInvariantString(double valor) =>
        valor.ToString("R", CultureInfo.InvariantCulture);
}
