using Cotizador3D.Core.Formatting;

namespace Cotizador3D.Core.Models;

/// <summary>Una linea del desglose que ve el cliente.</summary>
/// <param name="Concepto">Rotulo en espanol.</param>
/// <param name="Importe">Importe ya escalado y redondeado a 2 decimales.</param>
public sealed record ClientQuoteLine(string Concepto, double Importe);

/// <summary>
/// Desglose para el cliente (docs/DECISIONS.md, seccion "PDF para el cliente").
/// Cada linea de costo se muestra multiplicada por el margen de ganancia, de
/// modo que las lineas suman exactamente <c>precio_venta</c>; el envio se
/// agrega como linea aparte (solo si es mayor a cero) y el total es
/// <c>precio_final_con_envio</c>. Nunca se expone el margen de ganancia ni el
/// costo real.
/// <para>
/// Todos los importes se redondean a 2 decimales al construir el desglose (los
/// mismos 2 decimales con que se imprimen), asi la columna impresa suma
/// exactamente el total impreso. El total se ancla en
/// <c>round(precio_final_con_envio)</c> (lo mismo que muestra la ventana
/// principal) y el residuo del redondeo lo absorbe la linea de costo MAS
/// GRANDE de las que se muestran: ninguna linea se vuelve negativa por un
/// centavo de redondeo.
/// </para>
/// </summary>
public sealed class ClientQuoteBreakdown
{
    public const string ConceptoMaterial = "Material";
    public const string ConceptoLuz = "Energía eléctrica";
    public const string ConceptoDesgaste = "Desgaste de la máquina";
    public const string ConceptoMargenError = "Margen de error";
    public const string ConceptoEnvio = "Envío";

    /// <summary>Magnitud maxima que se suma en decimal sin riesgo de desborde.</summary>
    private const double LimiteSumaExacta = 1e15;

    private ClientQuoteBreakdown(
        double material,
        double luz,
        double ivaLuz,
        double desgaste,
        double margenError,
        bool mostrarMargenError,
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
        };

        if (mostrarMargenError)
        {
            lineas.Add(new ClientQuoteLine(ConceptoMargenError, margenError));
        }

        if (envio > 0)
        {
            lineas.Add(new ClientQuoteLine(ConceptoEnvio, envio));
        }

        Lineas = lineas;
    }

    /// <summary>
    /// Material ya escalado por el margen, redondeado a 2 decimales. Como toda
    /// linea de costo, puede llevar el residuo del redondeo si resulta ser la
    /// mas grande del desglose.
    /// </summary>
    public double Material { get; }

    /// <summary>Energia electrica (sin IVA) ya escalada por el margen, redondeada a 2 decimales.</summary>
    public double Luz { get; }

    /// <summary>IVA de la energia ya escalado por el margen, redondeado a 2 decimales.</summary>
    public double IvaLuz { get; }

    /// <summary>Desgaste de la maquina ya escalado por el margen, redondeado a 2 decimales.</summary>
    public double Desgaste { get; }

    /// <summary>
    /// Margen de error ya escalado por el margen, redondeado a 2 decimales.
    /// Vale 0 cuando <c>margen_error_pct</c> es 0, y en ese caso la linea NO se
    /// emite (misma regla que el envio) ni participa del reparto del residuo.
    /// </summary>
    public double MargenError { get; }

    /// <summary>Costo de envio redondeado (no se escala: se suma despues del margen).</summary>
    public double Envio { get; }

    /// <summary>Total a pagar: <c>precio_final_con_envio</c> redondeado a 2 decimales.</summary>
    public double Total { get; }

    /// <summary>Rotulo de la linea de IVA, con el porcentaje: "IVA energía (21%)".</summary>
    public string ConceptoIvaLuz { get; }

    /// <summary>
    /// Lineas a mostrar, en orden. Incluye el margen de error solo si el
    /// porcentaje configurado es mayor a cero, y el envio solo si es mayor a cero.
    /// </summary>
    public IReadOnlyList<ClientQuoteLine> Lineas { get; }

    /// <summary>
    /// Suma de todas las lineas; coincide exactamente con <see cref="Total"/>.
    /// Se suma en <see cref="decimal"/> porque los importes ya son valores de
    /// 2 decimales: asi la suma no arrastra residuo binario.
    /// </summary>
    public double SumaDeLineas => Sumar(Lineas.Select(l => l.Importe).ToList());

    /// <summary>Construye el desglose del cliente a partir de un resultado de calculo.</summary>
    public static ClientQuoteBreakdown Crear(QuoteResult resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);

        var m = resultado.MargenGanancia;

        // Se redondea cada linea a los mismos 2 decimales con que se imprime.
        var margenError = Redondear(resultado.MargenErrorValor * m);

        // Con margen de error 0% la linea no aporta nada: no se emite (misma
        // regla que el envio), asi nunca se imprime un "$ -0,01" de residuo.
        var mostrarMargenError = resultado.MargenErrorPct > 0;

        // El total es el precio final redondeado: exactamente el importe que
        // muestra la ventana principal.
        var total = Redondear(resultado.PrecioFinalConEnvio);
        var envio = Redondear(resultado.CostoEnvio);
        var precioVenta = Restar(total, envio);

        // Lineas de costo que se emiten, en orden de aparicion.
        var costos = mostrarMargenError
            ? new[]
            {
                Redondear(resultado.PrecioMaterial * m),
                Redondear(resultado.PrecioLuz * m),
                Redondear(resultado.IvaLuzValor * m),
                Redondear(resultado.DesgasteMaquina * m),
                margenError,
            }
            : new[]
            {
                Redondear(resultado.PrecioMaterial * m),
                Redondear(resultado.PrecioLuz * m),
                Redondear(resultado.IvaLuzValor * m),
                Redondear(resultado.DesgasteMaquina * m),
            };

        RepartirResiduo(costos, Restar(precioVenta, Sumar(costos)));

        if (mostrarMargenError)
        {
            margenError = costos[4];
        }

        var conceptoIva = $"IVA energía ({MoneyFormat.Porcentaje(resultado.IvaLuzPct)}%)";

        return new ClientQuoteBreakdown(
            costos[0],
            costos[1],
            costos[2],
            costos[3],
            margenError,
            mostrarMargenError,
            envio,
            total,
            conceptoIva);
    }

    /// <summary>
    /// Suma el residuo del redondeo a la linea de costo MAS GRANDE, que es la
    /// que puede absorberlo sin distorsionarse (unos centavos sobre el importe
    /// mayor). Si el residuo es negativo y no entra entero en esa linea, sigue
    /// por la siguiente mas grande: asi la suma cierra exacta y ninguna linea
    /// queda negativa (el total de las lineas nunca es menor al residuo a
    /// descontar, porque el precio de venta no es negativo).
    /// </summary>
    private static void RepartirResiduo(double[] costos, double residuo)
    {
        var porTamano = Enumerable.Range(0, costos.Length)
            .OrderByDescending(i => costos[i])
            .ToList();

        foreach (var i in porTamano)
        {
            if (residuo == 0d)
            {
                return;
            }

            // El residuo positivo entra entero; el negativo, hasta dejar la
            // linea en cero.
            var ajuste = residuo > 0d ? residuo : Math.Max(residuo, -costos[i]);

            costos[i] = Sumar(new[] { costos[i], ajuste });
            residuo = Restar(residuo, ajuste);
        }
    }

    /// <summary>
    /// Redondea a 2 decimales (medio hacia arriba, como espera el cliente) y
    /// lleva a cero los importes despreciables, para no imprimir "-0,00".
    /// </summary>
    private static double Redondear(double valor)
    {
        if (double.IsNaN(valor) || Math.Abs(valor) < 0.005d)
        {
            return 0d;
        }

        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Suma importes de 2 decimales sin residuo binario: convierte a
    /// <see cref="decimal"/>, suma exacto y vuelve a <see cref="double"/>. Con
    /// importes absurdamente grandes (fuera del rango util de decimal) se suma
    /// en double: el desglose deja de cerrar al centavo, pero no revienta.
    /// </summary>
    private static double Sumar(IReadOnlyCollection<double> importes)
    {
        if (importes.Any(i => !double.IsFinite(i) || Math.Abs(i) > LimiteSumaExacta))
        {
            return importes.Sum();
        }

        var total = 0m;
        foreach (var importe in importes)
        {
            total += (decimal)importe;
        }

        return (double)total;
    }

    /// <summary>Resta dos importes de 2 decimales, con la exactitud de <see cref="Sumar"/>.</summary>
    private static double Restar(double minuendo, double sustraendo) =>
        Sumar(new[] { minuendo, -sustraendo });
}
