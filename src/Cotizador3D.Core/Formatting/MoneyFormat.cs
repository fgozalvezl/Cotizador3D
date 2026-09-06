using System.Globalization;

namespace Cotizador3D.Core.Formatting;

/// <summary>
/// Formato numerico de la app. El legado usaba el formato C ("$ 1,234.56");
/// por decision B9 se pasa al formato es-AR: "$ 1.234,56" (punto para miles,
/// coma para decimales, dos decimales siempre).
/// </summary>
public static class MoneyFormat
{
    /// <summary>
    /// Formato es-AR construido a mano para que no dependa de que el ICU del
    /// sistema tenga la cultura instalada.
    /// </summary>
    public static NumberFormatInfo CulturaEsAr { get; } = CrearFormatoEsAr();

    private static NumberFormatInfo CrearFormatoEsAr()
    {
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.NumberDecimalSeparator = ",";
        nfi.NumberGroupSeparator = ".";
        nfi.NumberGroupSizes = new[] { 3 };
        nfi.NumberNegativePattern = 1; // -n : el signo pegado al numero.
        nfi.NegativeSign = "-";
        return NumberFormatInfo.ReadOnly(nfi);
    }

    /// <summary>Valor monetario: "$ 1.234,56", "$ 0,00", "$ -1.234,50".</summary>
    public static string Moneda(double valor) => "$ " + Numero(valor);

    /// <summary>Valor monetario compacto para listas: "$1.234,56/kg" (sin espacio, como el legado).</summary>
    public static string MonedaPorKg(double valor) => "$" + Numero(valor) + "/kg";

    /// <summary>Numero con separador de miles y la cantidad de decimales indicada (2 por defecto).</summary>
    public static string Numero(double valor, int decimales = 2)
    {
        if (decimales < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimales));
        }

        // Todo lo que redondearia a cero se imprime como cero positivo: sin
        // esto, un residuo negativo minusculo sale como "-0,00".
        var umbralCero = 0.5d * Math.Pow(10d, -decimales);
        var normalizado = Math.Abs(valor) < umbralCero ? 0d : valor;
        return normalizado.ToString("N" + decimales.ToString(CultureInfo.InvariantCulture), CulturaEsAr);
    }

    /// <summary>
    /// Porcentaje sin ceros decimales sobrantes: 21 -> "21", 10.5 -> "10,5".
    /// Se usa para rotular "IVA Luz (21%)".
    /// </summary>
    public static string Porcentaje(double valor)
    {
        var texto = valor.ToString("0.##", CulturaEsAr);
        return texto == "-0" ? "0" : texto;
    }

    /// <summary>Horas decimales con dos decimales: 2.5 -> "2,50 h".</summary>
    public static string Horas(double horas) => Numero(horas) + " h";

    /// <summary>
    /// Duracion legible a partir de horas decimales, redondeando al segundo:
    /// 2.5 -> "2 h 30 min"; 26 -> "1 d 2 h"; 0.5 -> "30 min"; 0 -> "0 min".
    /// </summary>
    public static string Duracion(double horas)
    {
        var negativa = horas < 0;
        var totalSegundos = (long)Math.Round(Math.Abs(horas) * 3600d, MidpointRounding.AwayFromZero);

        var dias = totalSegundos / 86400;
        var restoHoras = totalSegundos % 86400 / 3600;
        var minutos = totalSegundos % 3600 / 60;
        var segundos = totalSegundos % 60;

        var partes = new List<string>(4);
        if (dias > 0)
        {
            partes.Add(dias.ToString(CultureInfo.InvariantCulture) + " d");
        }

        if (restoHoras > 0)
        {
            partes.Add(restoHoras.ToString(CultureInfo.InvariantCulture) + " h");
        }

        if (minutos > 0)
        {
            partes.Add(minutos.ToString(CultureInfo.InvariantCulture) + " min");
        }

        if (segundos > 0)
        {
            partes.Add(segundos.ToString(CultureInfo.InvariantCulture) + " s");
        }

        if (partes.Count == 0)
        {
            return "0 min";
        }

        var texto = string.Join(" ", partes);
        return negativa ? "-" + texto : texto;
    }
}
