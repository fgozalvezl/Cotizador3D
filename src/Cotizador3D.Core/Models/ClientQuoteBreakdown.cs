using Cotizador3D.Core.Formatting;

namespace Cotizador3D.Core.Models;

/// <summary>Una linea del desglose que ve el cliente.</summary>
/// <param name="Concepto">Rotulo en espanol.</param>
/// <param name="Importe">Importe ya escalado.</param>
public sealed record ClientQuoteLine(string Concepto, double Importe);

/// <summary>
/// Desglose para el cliente (docs/DECISIONS.md, seccion "PDF para el cliente").
/// Cada linea de costo se muestra multiplicada por el margen de ganancia, de
/// modo que las lineas suman exactamente <c>precio_venta</c>; el envio se
/// agrega como linea aparte (solo si es mayor a cero) y el total es
/// <c>precio_final_con_envio</c>. Nunca se expone el margen de ganancia ni el
/// costo real.
/// </summary>
public sealed class ClientQuoteBreakdown
{
    public const string ConceptoMaterial = "Material";
    public const string ConceptoLuz = "Energía eléctrica";
    public const string ConceptoDesgaste = "Desgaste de la máquina";
    public const string ConceptoMargenError = "Margen de error";
    public const string ConceptoEnvio = "Envío";

    private ClientQuoteBreakdown(
        double material,
        double luz,
        double ivaLuz,
        double desgaste,
        double margenError,
        double envio,
        double total,
        string conceptoIvaLuz)
    {
        Material = material;
        Luz = luz;
        IvaLuz = ivaLuz;
        Desgaste = desgaste;
        MargenError = margenError;
        Envio = envio;
        Total = total;
        ConceptoIvaLuz = conceptoIvaLuz;

        var lineas = new List<ClientQuoteLine>(6)
        {
            new(ConceptoMaterial, material),
            new(ConceptoLuz, luz),
            new(conceptoIvaLuz, ivaLuz),
            new(ConceptoDesgaste, desgaste),
            new(ConceptoMargenError, margenError),
        };

        if (envio > 0)
        {
            lineas.Add(new ClientQuoteLine(ConceptoEnvio, envio));
        }

        Lineas = lineas;
    }

    /// <summary>Material ya escalado por el margen.</summary>
    public double Material { get; }

    /// <summary>Energia electrica (sin IVA) ya escalada por el margen.</summary>
    public double Luz { get; }

    /// <summary>IVA de la energia ya escalado por el margen.</summary>
    public double IvaLuz { get; }

    /// <summary>Desgaste de la maquina ya escalado por el margen.</summary>
    public double Desgaste { get; }

    /// <summary>
    /// Margen de error ya escalado. Absorbe el residuo de coma flotante para
    /// que la suma de las lineas de costo sea exactamente el precio de venta.
    /// </summary>
    public double MargenError { get; }

    /// <summary>Costo de envio (no se escala: se suma despues del margen).</summary>
    public double Envio { get; }

    /// <summary>Total a pagar: precio_final_con_envio.</summary>
    public double Total { get; }

    /// <summary>Rotulo de la linea de IVA, con el porcentaje: "IVA energía (21%)".</summary>
    public string ConceptoIvaLuz { get; }

    /// <summary>Lineas a mostrar, en orden. Incluye el envio solo si es mayor a cero.</summary>
    public IReadOnlyList<ClientQuoteLine> Lineas { get; }

    /// <summary>Suma de todas las lineas; coincide con <see cref="Total"/>.</summary>
    public double SumaDeLineas => Lineas.Sum(l => l.Importe);

    /// <summary>Construye el desglose del cliente a partir de un resultado de calculo.</summary>
    public static ClientQuoteBreakdown Crear(QuoteResult resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        var m = resultado.MargenGanancia;
        var material = resultado.PrecioMaterial * m;
        var luz = resultado.PrecioLuz * m;
        var ivaLuz = resultado.IvaLuzValor * m;
        var desgaste = resultado.DesgasteMaquina * m;

        // El margen de error cierra la suma: se calcula como residuo sobre la
        // misma suma parcial (y en el mismo orden) con que se recorren las
        // lineas, para que el desglose de exactamente precio_venta pese al
        // redondeo binario de los productos anteriores.
        var sumaParcial = material + luz + ivaLuz + desgaste;
        var margenError = resultado.PrecioVenta - sumaParcial;

        var conceptoIva = $"IVA energía ({MoneyFormat.Porcentaje(resultado.IvaLuzPct)}%)";

        return new ClientQuoteBreakdown(
            material,
            luz,
            ivaLuz,
            desgaste,
            margenError,
            resultado.CostoEnvio,
            resultado.PrecioFinalConEnvio,
            conceptoIva);
    }
}
