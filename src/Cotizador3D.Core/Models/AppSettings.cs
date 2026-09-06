namespace Cotizador3D.Core.Models;

/// <summary>
/// Parametros de configuracion del usuario. Se corresponden 1 a 1 con el objeto
/// "settings" del JSON legado (docs/SPEC-legacy.md 1.2.1). Los valores se
/// manejan como <see cref="double"/> para reproducir exactamente la semantica
/// de los float de Python.
/// </summary>
public sealed class AppSettings
{
    public const double PrecioKwhPorDefecto = 199.7464;
    public const double ConsumoWPorDefecto = 150;
    public const double DesgasteHorasPorDefecto = 5000;
    public const double PrecioRepuestosPorDefecto = 305000;
    public const double MargenErrorPctPorDefecto = 10;
    public const double IvaLuzPctPorDefecto = 21;
    public const double MargenGananciaPorDefecto = 1.5;
    public const double CostoEnvioPorDefecto = 0;
    public const string NombreNegocioPorDefecto = "";

    /// <summary>Precio del kWh. Clave JSON: <c>precio_kwh</c>.</summary>
    public double PrecioKwh { get; set; } = PrecioKwhPorDefecto;

    /// <summary>Consumo real de la impresora en watts. Clave JSON: <c>consumo_w</c>.</summary>
    public double ConsumoW { get; set; } = ConsumoWPorDefecto;

    /// <summary>Vida util de la maquina en horas. Clave JSON: <c>desgaste_horas</c>.</summary>
    public double DesgasteHoras { get; set; } = DesgasteHorasPorDefecto;

    /// <summary>Costo total de repuestos. Clave JSON: <c>precio_repuestos</c>.</summary>
    public double PrecioRepuestos { get; set; } = PrecioRepuestosPorDefecto;

    /// <summary>Margen de error en porcentaje (0-100). Clave JSON: <c>margen_error_pct</c>.</summary>
    public double MargenErrorPct { get; set; } = MargenErrorPctPorDefecto;

    /// <summary>IVA aplicado al costo de luz, en porcentaje. Clave JSON: <c>iva_luz_pct</c>.</summary>
    public double IvaLuzPct { get; set; } = IvaLuzPctPorDefecto;

    /// <summary>Multiplicador de ganancia (1.5 = +50 %). Clave JSON: <c>margen_ganancia_x</c>.</summary>
    public double MargenGanancia { get; set; } = MargenGananciaPorDefecto;

    /// <summary>Costo fijo de envio, se suma despues del margen. Clave JSON: <c>costo_envio</c>.</summary>
    public double CostoEnvio { get; set; } = CostoEnvioPorDefecto;

    /// <summary>Geometria "ANCHOxALTO" de la ventana principal. Clave JSON: <c>geometry</c>.</summary>
    public string Geometry { get; set; } = CoreConstants.GeometryPorDefecto;

    /// <summary>Nombre del negocio para el PDF (clave nueva). Clave JSON: <c>nombre_negocio</c>.</summary>
    public string NombreNegocio { get; set; } = NombreNegocioPorDefecto;

    /// <summary>Copia superficial e independiente de esta configuracion.</summary>
    public AppSettings Clonar() => new()
    {
        PrecioKwh = PrecioKwh,
        ConsumoW = ConsumoW,
        DesgasteHoras = DesgasteHoras,
        PrecioRepuestos = PrecioRepuestos,
        MargenErrorPct = MargenErrorPct,
        IvaLuzPct = IvaLuzPct,
        MargenGanancia = MargenGanancia,
        CostoEnvio = CostoEnvio,
        Geometry = Geometry,
        NombreNegocio = NombreNegocio,
    };
}
