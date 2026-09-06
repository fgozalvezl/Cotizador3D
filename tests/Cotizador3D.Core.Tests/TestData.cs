using Cotizador3D.Core.Models;

namespace Cotizador3D.Core.Tests;

/// <summary>Datos compartidos por los tests, con los valores por defecto del legado.</summary>
internal static class TestData
{
    /// <summary>Configuracion con los defaults de docs/SPEC-legacy.md 1.2.1.</summary>
    public static AppSettings SettingsPorDefecto() => new()
    {
        PrecioKwh = 199.7464,
        ConsumoW = 150,
        DesgasteHoras = 5000,
        PrecioRepuestos = 305000,
        MargenErrorPct = 10,
        IvaLuzPct = 21,
        MargenGanancia = 1.5,
        CostoEnvio = 0,
        Geometry = "950x700",
        NombreNegocio = string.Empty,
    };

    /// <summary>Filamento de referencia: 25.000 por kilo.</summary>
    public static Filament FilamentoPorDefecto() => new()
    {
        Id = "grilon3_pla_9f3c1a02",
        Brand = "Grilon3",
        Type = "PLA",
        PriceKg = 25000,
    };

    /// <summary>Trabajo de referencia: 100 g, 2 h 30 min, con los defaults.</summary>
    public static QuoteInput EntradaPorDefecto() => new()
    {
        Filamento = FilamentoPorDefecto(),
        Gramos = 100,
        Dias = 0,
        Horas = 2,
        Minutos = 30,
        Segundos = 0,
        Settings = SettingsPorDefecto(),
    };
}
