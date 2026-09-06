using System.Globalization;
using System.Text.RegularExpressions;
using Cotizador3D.Core.Formatting;

namespace Cotizador3D.Core.Tests;

/// <summary>
/// Lee importes en el formato en que los IMPRIME la app ("$ 1.234,56", es-AR).
/// Se usa para comprobar sobre el texto ya formateado (UI o PDF) que la columna
/// de importes suma exactamente el total, que es lo que ve el cliente.
/// </summary>
internal static class ImportesDelTexto
{
    private static readonly Regex Importe = new(
        @"\$\s*(?<valor>-?\d{1,3}(?:\.\d{3})*,\d{2})",
        RegexOptions.CultureInvariant);

    /// <summary>Todos los importes "$ ..." del texto, en orden de aparicion.</summary>
    public static IReadOnlyList<decimal> Extraer(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        return Importe.Matches(texto)
            .Select(m => Parsear(m.Groups["valor"].Value))
            .ToList();
    }

    /// <summary>Parsea un importe es-AR ("1.234,56") como decimal exacto.</summary>
    public static decimal Parsear(string valor) =>
        decimal.Parse(valor, NumberStyles.Number, MoneyFormat.CulturaEsAr);

    /// <summary>El mismo valor que imprime la app, ya parseado a decimal.</summary>
    public static decimal ComoSeImprime(double valor) =>
        Parsear(MoneyFormat.Numero(valor));
}
