namespace Cotizador3D.Core.Models;

/// <summary>
/// Resultado completo de un calculo, con todos los valores intermedios de
/// docs/SPEC-legacy.md 2.4. Sin redondeo: los valores se redondean solo al
/// mostrarlos.
/// </summary>
public sealed class QuoteResult
{
    /// <summary>Tiempo total de impresion en horas decimales (2.2).</summary>
    public required double HorasImpresion { get; init; }

    /// <summary>(gramos / 1000) * precio_kg.</summary>
    public required double PrecioMaterial { get; init; }

    /// <summary>(consumo_w / 1000) * horas * precio_kwh. Sin IVA.</summary>
    public required double PrecioLuz { get; init; }

    /// <summary>(costo_repuestos / vida_util_horas) * horas; 0 si la vida util es &lt;= 0.</summary>
    public required double DesgasteMaquina { get; init; }

    /// <summary>material + luz + desgaste.</summary>
    public required double CostoBase { get; init; }

    /// <summary>costo_base * (margen_error_pct / 100).</summary>
    public required double MargenErrorValor { get; init; }

    /// <summary>precio_luz * (iva_luz_pct / 100).</summary>
    public required double IvaLuzValor { get; init; }

    /// <summary>costo_base + margen_error_valor + iva_luz_valor.</summary>
    public required double CostoTotal { get; init; }

    /// <summary>costo_total * margen_ganancia.</summary>
    public required double PrecioVenta { get; init; }

    /// <summary>precio_venta + costo_envio (el envio NO se multiplica por el margen).</summary>
    public required double PrecioFinalConEnvio { get; init; }

    /// <summary>Costo de envio usado en el calculo.</summary>
    public required double CostoEnvio { get; init; }

    /// <summary>Multiplicador de ganancia usado. NUNCA debe mostrarse al cliente.</summary>
    public required double MargenGanancia { get; init; }

    /// <summary>Porcentaje de IVA aplicado a la luz, para rotular el desglose.</summary>
    public required double IvaLuzPct { get; init; }

    /// <summary>Porcentaje de margen de error aplicado, para rotular el desglose.</summary>
    public required double MargenErrorPct { get; init; }

    /// <summary>
    /// Desglose escalado para el cliente (docs/DECISIONS.md): cada linea de
    /// costo se multiplica por el margen de ganancia, el envio va aparte y el
    /// total es <see cref="PrecioFinalConEnvio"/>.
    /// </summary>
    public ClientQuoteBreakdown ParaCliente() => ClientQuoteBreakdown.Crear(this);
}
