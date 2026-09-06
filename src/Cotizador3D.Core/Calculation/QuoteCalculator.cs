using Cotizador3D.Core.Models;

namespace Cotizador3D.Core.Calculation;

/// <summary>
/// Implementa las formulas de docs/SPEC-legacy.md 2.2 a 2.4, en el mismo orden
/// y sin redondeo intermedio (todo en <see cref="double"/>).
/// </summary>
public static class QuoteCalculator
{
    /// <summary>Calcula una cotizacion completa.</summary>
    /// <exception cref="QuoteValidationException">Si alguna entrada es invalida.</exception>
    public static QuoteResult Calcular(QuoteInput entrada)
    {
        ArgumentNullException.ThrowIfNull(entrada);

        var settings = entrada.Settings ?? throw new QuoteValidationException(
            "Falta la configuración para calcular la cotización.", "settings");

        var filamento = entrada.Filamento ?? throw new QuoteValidationException(
            "Filamento no válido o no seleccionado.", "filamento");

        // B18: los valores negativos se rechazan con un mensaje claro.
        ValidarNoNegativo(entrada.Gramos, "Los gramos de material no pueden ser negativos.", "gramos");
        ValidarNoNegativo(entrada.Dias, "Los días no pueden ser negativos.", "dias");
        ValidarNoNegativo(entrada.Horas, "Las horas no pueden ser negativas.", "horas");
        ValidarNoNegativo(entrada.Minutos, "Los minutos no pueden ser negativos.", "minutos");
        ValidarNoNegativo(entrada.Segundos, "Los segundos no pueden ser negativos.", "segundos");
        ValidarNoNegativo(filamento.PriceKg, "El precio por kilo del filamento no puede ser negativo.", "price_kg");
        ValidarNoNegativo(settings.PrecioKwh, "El precio del kWh no puede ser negativo.", "precio_kwh");
        ValidarNoNegativo(settings.ConsumoW, "El consumo en watts no puede ser negativo.", "consumo_w");
        ValidarNoNegativo(settings.DesgasteHoras, "La vida útil de la máquina no puede ser negativa.", "desgaste_horas");
        ValidarNoNegativo(settings.PrecioRepuestos, "El costo de repuestos no puede ser negativo.", "precio_repuestos");
        ValidarNoNegativo(settings.MargenErrorPct, "El margen de error no puede ser negativo.", "margen_error_pct");
        ValidarNoNegativo(settings.IvaLuzPct, "El IVA de la energía no puede ser negativo.", "iva_luz_pct");
        ValidarNoNegativo(settings.MargenGanancia, "El margen de ganancia no puede ser negativo.", "margen_ganancia_x");
        ValidarNoNegativo(settings.CostoEnvio, "El costo de envío no puede ser negativo.", "costo_envio");

        var horasImpresion = CalcularHoras(entrada.Dias, entrada.Horas, entrada.Minutos, entrada.Segundos);

        if (horasImpresion <= 0)
        {
            throw new QuoteValidationException(
                "El tiempo de impresión debe ser mayor a cero.", "tiempo");
        }

        if (entrada.Gramos <= 0)
        {
            throw new QuoteValidationException(
                "Los gramos de material deben ser mayores a cero.", "gramos");
        }

        var precioMaterial = entrada.Gramos / 1000d * filamento.PriceKg;
        var precioLuz = settings.ConsumoW / 1000d * horasImpresion * settings.PrecioKwh;

        // B7: si la vida útil es <= 0 no hay desgaste (la UI muestra un aviso).
        var desgasteMaquina = settings.DesgasteHoras > 0
            ? settings.PrecioRepuestos / settings.DesgasteHoras * horasImpresion
            : 0d;

        var costoBase = precioMaterial + precioLuz + desgasteMaquina;
        var margenErrorValor = costoBase * (settings.MargenErrorPct / 100d);
        var ivaLuzValor = precioLuz * (settings.IvaLuzPct / 100d);
        var costoTotal = costoBase + margenErrorValor + ivaLuzValor;
        var precioVenta = costoTotal * settings.MargenGanancia;
        var precioFinalConEnvio = precioVenta + settings.CostoEnvio;

        return new QuoteResult
        {
            HorasImpresion = horasImpresion,
            PrecioMaterial = precioMaterial,
            PrecioLuz = precioLuz,
            DesgasteMaquina = desgasteMaquina,
            CostoBase = costoBase,
            MargenErrorValor = margenErrorValor,
            IvaLuzValor = ivaLuzValor,
            CostoTotal = costoTotal,
            PrecioVenta = precioVenta,
            PrecioFinalConEnvio = precioFinalConEnvio,
            CostoEnvio = settings.CostoEnvio,
            MargenGanancia = settings.MargenGanancia,
            IvaLuzPct = settings.IvaLuzPct,
            MargenErrorPct = settings.MargenErrorPct,
        };
    }

    /// <summary>
    /// Tiempo total en horas decimales: (dias * 24) + horas + (minutos / 60) +
    /// (segundos / 3600), en ese orden exacto.
    /// </summary>
    public static double CalcularHoras(double dias, double horas, double minutos, double segundos) =>
        dias * 24d + horas + minutos / 60d + segundos / 3600d;

    /// <summary>
    /// Indica si el desgaste de máquina quedó anulado por una vida útil no
    /// positiva (B7: se calcula igual, pero la UI debe avisar).
    /// </summary>
    public static bool DesgasteAnulado(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.DesgasteHoras <= 0;
    }

    private static void ValidarNoNegativo(double valor, string mensaje, string campo)
    {
        if (double.IsNaN(valor))
        {
            throw new QuoteValidationException($"El valor de \"{campo}\" no es un número válido.", campo);
        }

        if (valor < 0)
        {
            throw new QuoteValidationException(mensaje, campo);
        }
    }
}
