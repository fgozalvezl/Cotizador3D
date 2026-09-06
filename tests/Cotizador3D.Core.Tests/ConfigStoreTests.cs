using System.Text.Json;
using Cotizador3D.Core;
using Cotizador3D.Core.Models;
using Cotizador3D.Core.Persistence;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class ConfigStoreTests
{
    /// <summary>Archivo tal cual lo escribe la app Python la primera vez (todo string).</summary>
    private const string JsonLegadoConStrings = """
    {
        "settings": {
            "precio_kwh": "199.74640",
            "consumo_w": "150",
            "desgaste_horas": "5000",
            "precio_repuestos": "305000",
            "margen_error_pct": "10",
            "iva_luz_pct": "21",
            "margen_ganancia_x": "1.5",
            "costo_envio": "0",
            "geometry": "950x700"
        },
        "filaments": [
            {
                "brand": "Grilon3",
                "type": "PLA",
                "price_kg": 18500.0,
                "id": "grilon3_pla_9f3c1a02"
            }
        ]
    }
    """;

    /// <summary>Archivo tal cual queda despues del autoguardado de la app Python (numeros).</summary>
    private const string JsonLegadoConNumeros = """
    {
        "settings": {
            "precio_kwh": 199.7464,
            "consumo_w": 150.0,
            "desgaste_horas": 5000.0,
            "precio_repuestos": 305000.0,
            "margen_error_pct": 10.0,
            "iva_luz_pct": "21",
            "margen_ganancia_x": 1.5,
            "costo_envio": 0.0,
            "geometry": "1024x740"
        },
        "filaments": [
            {
                "brand": "Grilon3",
                "type": "PLA",
                "price_kg": 18500.0,
                "id": "grilon3_pla_9f3c1a02"
            }
        ]
    }
    """;

    [Fact]
    public void ObtenerRutaPorDefecto_UsaLocalAppData()
    {
        var anterior = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        try
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", Path.Combine(Path.GetTempPath(), "LocalAppDataFalso"));
            var ruta = ConfigStore.ObtenerRutaPorDefecto();

            Assert.Equal(
                Path.Combine(Path.GetTempPath(), "LocalAppDataFalso", CoreConstants.NombreDirectorioApp, CoreConstants.NombreArchivoConfig),
                ruta);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", anterior);
        }
    }

    [Fact]
    public void ObtenerRutaPorDefecto_SinLocalAppData_CaeAlHome()
    {
        var anterior = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        try
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", null);
            var ruta = ConfigStore.ObtenerRutaPorDefecto();

            Assert.EndsWith(
                Path.Combine(CoreConstants.NombreDirectorioApp, CoreConstants.NombreArchivoConfig),
                ruta,
                StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", anterior);
        }
    }

    [Fact]
    public void Cargar_ArchivoInexistente_DevuelveDefaults()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        AssertSonLosDefaults(datos);
        Assert.False(File.Exists(temp.Archivo()));
    }

    [Fact]
    public void Cargar_NoCreaElDirectorio()
    {
        using var temp = new DirectorioTemporal();
        var subdirectorio = Path.Combine(temp.Ruta, "Cotizador3D");
        var store = new ConfigStore(Path.Combine(subdirectorio, CoreConstants.NombreArchivoConfig));

        store.Cargar();

        Assert.False(Directory.Exists(subdirectorio));
    }

    [Fact]
    public void Guardar_CreaElDirectorio()
    {
        using var temp = new DirectorioTemporal();
        var subdirectorio = Path.Combine(temp.Ruta, "Cotizador3D");
        var store = new ConfigStore(Path.Combine(subdirectorio, CoreConstants.NombreArchivoConfig));

        store.Guardar(AppData.PorDefecto());

        Assert.True(File.Exists(store.RutaArchivo));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \n  ")]
    [InlineData("{ esto no es json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("42")]
    public void Cargar_ContenidoInvalido_DevuelveDefaults(string contenido)
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), contenido);
        var store = new ConfigStore(temp.Archivo());

        AssertSonLosDefaults(store.Cargar());
    }

    [Fact]
    public void Cargar_SinClaveSettings_DevuelveDefaultsYConservaFilamentos()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """{"filaments":[{"brand":"X","type":"PLA","price_kg":100}]}""");
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        AssertSonLosDefaults(datos, esperarFilamentosVacios: false);
        Assert.Single(datos.Filaments);
    }

    [Fact]
    public void Cargar_ClavesFaltantes_TomanElDefault()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """{"settings":{"consumo_w":"200"}}""");
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal(200d, datos.Settings.ConsumoW);
        Assert.Equal(199.7464d, datos.Settings.PrecioKwh, 10);
        Assert.Equal(5000d, datos.Settings.DesgasteHoras);
        Assert.Equal(21d, datos.Settings.IvaLuzPct);
        Assert.Equal(1.5d, datos.Settings.MargenGanancia);
        Assert.Equal(0d, datos.Settings.CostoEnvio);
        Assert.Equal(CoreConstants.GeometryPorDefecto, datos.Settings.Geometry);
        Assert.Equal(string.Empty, datos.Settings.NombreNegocio);
        Assert.Empty(datos.Filaments);
    }

    [Fact]
    public void Cargar_ValorNumericoBasura_TomaElDefault()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """{"settings":{"precio_kwh":"no es un numero","consumo_w":null}}""");
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal(199.7464d, datos.Settings.PrecioKwh, 10);
        Assert.Equal(150d, datos.Settings.ConsumoW);
    }

    [Fact]
    public void Cargar_JsonLegadoConStrings_SeLeeCompleto()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), JsonLegadoConStrings);
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal(199.7464d, datos.Settings.PrecioKwh, 10);
        Assert.Equal(150d, datos.Settings.ConsumoW);
        Assert.Equal(5000d, datos.Settings.DesgasteHoras);
        Assert.Equal(305000d, datos.Settings.PrecioRepuestos);
        Assert.Equal(10d, datos.Settings.MargenErrorPct);
        Assert.Equal(21d, datos.Settings.IvaLuzPct);
        Assert.Equal(1.5d, datos.Settings.MargenGanancia);
        Assert.Equal(0d, datos.Settings.CostoEnvio);
        Assert.Equal("950x700", datos.Settings.Geometry);

        var filamento = Assert.Single(datos.Filaments);
        Assert.Equal("Grilon3", filamento.Brand);
        Assert.Equal("PLA", filamento.Type);
        Assert.Equal(18500d, filamento.PriceKg);
        Assert.Equal("grilon3_pla_9f3c1a02", filamento.Id);
    }

    [Fact]
    public void Cargar_JsonLegadoConNumeros_SeLeeCompleto()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), JsonLegadoConNumeros);
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal(199.7464d, datos.Settings.PrecioKwh, 10);
        Assert.Equal(150d, datos.Settings.ConsumoW);
        Assert.Equal(1.5d, datos.Settings.MargenGanancia);
        Assert.Equal(21d, datos.Settings.IvaLuzPct);
        Assert.Equal("1024x740", datos.Settings.Geometry);
        Assert.Equal(18500d, Assert.Single(datos.Filaments).PriceKg);
    }

    [Fact]
    public void Cargar_FilamentoSinId_RecibeUnIdNuevo()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """
        {"settings":{},"filaments":[
            {"brand":"A","type":"PLA","price_kg":100},
            {"brand":"B","type":"PETG","price_kg":"200"},
            {"brand":"C","type":"TPU","price_kg":300,"id":""},
            {"brand":"D","type":"ABS","price_kg":400,"id":"   "}
        ]}
        """);
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal(4, datos.Filaments.Count);
        Assert.All(datos.Filaments, f => Assert.False(string.IsNullOrWhiteSpace(f.Id)));
        Assert.Equal(4, datos.Filaments.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(200d, datos.Filaments[1].PriceKg);
    }

    [Fact]
    public void Cargar_IdLegado_SeConservaTalCual()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """
        {"settings":{},"filaments":[
            {"brand":"Grilon 3","type":"PLA","price_kg":100,"id":"grilon_3_pla_a1b2c3d4"},
            {"brand":"B","type":"PETG","price_kg":200,"id":"9f3c1a02-0000-0000-0000-000000000000"}
        ]}
        """);
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal("grilon_3_pla_a1b2c3d4", datos.Filaments[0].Id);
        Assert.Equal("9f3c1a02-0000-0000-0000-000000000000", datos.Filaments[1].Id);
    }

    [Fact]
    public void GuardarYCargar_ConservaElIdLegadoSinCambios()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), JsonLegadoConStrings);
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();
        store.Guardar(datos);

        Assert.Equal("grilon3_pla_9f3c1a02", Assert.Single(store.Cargar().Filaments).Id);
        Assert.Contains("grilon3_pla_9f3c1a02", File.ReadAllText(temp.Archivo()), StringComparison.Ordinal);
    }

    [Fact]
    public void Cargar_IdsDuplicados_ElPrimeroGanaYElRestoRecibeUnoNuevo()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """
        {"settings":{},"filaments":[
            {"brand":"A","type":"PLA","price_kg":100,"id":"repetido"},
            {"brand":"B","type":"PETG","price_kg":200,"id":"repetido"},
            {"brand":"C","type":"TPU","price_kg":300,"id":"repetido"}
        ]}
        """);
        var store = new ConfigStore(temp.Archivo());

        var datos = store.Cargar();

        Assert.Equal(3, datos.Filaments.Count);
        Assert.Equal("repetido", datos.Filaments[0].Id);
        Assert.NotEqual("repetido", datos.Filaments[1].Id);
        Assert.NotEqual("repetido", datos.Filaments[2].Id);
        Assert.Equal(3, datos.Filaments.Select(f => f.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("A", datos.BuscarFilamento("repetido")!.Brand);
    }

    [Fact]
    public void Cargar_ElementosNoObjeto_SeIgnoran()
    {
        using var temp = new DirectorioTemporal();
        File.WriteAllText(temp.Archivo(), """{"settings":{},"filaments":[1,"x",null,{"brand":"A","type":"PLA","price_kg":100}]}""");
        var store = new ConfigStore(temp.Archivo());

        Assert.Single(store.Cargar().Filaments);
    }

    [Fact]
    public void GuardarYCargar_RoundTrip()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());
        var original = new AppData
        {
            Settings = new AppSettings
            {
                PrecioKwh = 199.7464,
                ConsumoW = 150,
                DesgasteHoras = 5000,
                PrecioRepuestos = 305000,
                MargenErrorPct = 10,
                IvaLuzPct = 21,
                MargenGanancia = 1.5,
                CostoEnvio = 3500.75,
                Geometry = "1024x740",
                NombreNegocio = "Impresiones Ñandú",
            },
            Filaments =
            {
                new Filament { Brand = "Grilon3", Type = "PLA", PriceKg = 18500.5 },
                new Filament { Brand = "Print A Lot", Type = "PETG-CF", PriceKg = 32000 },
            },
        };

        store.Guardar(original);
        var recargado = store.Cargar();

        Assert.Equal(original.Settings.PrecioKwh, recargado.Settings.PrecioKwh, 10);
        Assert.Equal(original.Settings.CostoEnvio, recargado.Settings.CostoEnvio, 10);
        Assert.Equal(original.Settings.Geometry, recargado.Settings.Geometry);
        Assert.Equal(original.Settings.NombreNegocio, recargado.Settings.NombreNegocio);
        Assert.Equal(2, recargado.Filaments.Count);
        Assert.Equal(original.Filaments[0].Id, recargado.Filaments[0].Id);
        Assert.Equal(original.Filaments[1].Brand, recargado.Filaments[1].Brand);
        Assert.Equal(32000d, recargado.Filaments[1].PriceKg);
    }

    [Fact]
    public void Guardar_EscribeLosValoresComoString()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());

        store.Guardar(AppData.PorDefecto());

        using var documento = JsonDocument.Parse(File.ReadAllText(temp.Archivo()));
        var settings = documento.RootElement.GetProperty("settings");

        foreach (var clave in new[]
                 {
                     "precio_kwh", "consumo_w", "desgaste_horas", "precio_repuestos",
                     "margen_error_pct", "iva_luz_pct", "margen_ganancia_x", "costo_envio",
                     "geometry", "nombre_negocio",
                 })
        {
            Assert.Equal(JsonValueKind.String, settings.GetProperty(clave).ValueKind);
        }

        Assert.Equal("199.7464", settings.GetProperty("precio_kwh").GetString());
        Assert.Equal("150", settings.GetProperty("consumo_w").GetString());
        Assert.Equal("1.5", settings.GetProperty("margen_ganancia_x").GetString());
        Assert.Equal("0", settings.GetProperty("costo_envio").GetString());
        Assert.Equal("950x700", settings.GetProperty("geometry").GetString());
        Assert.Equal(JsonValueKind.Array, documento.RootElement.GetProperty("filaments").ValueKind);
    }

    [Fact]
    public void Guardar_ElFilamentoConservaElEsquemaLegado()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());
        var datos = AppData.PorDefecto();
        datos.Filaments.Add(new Filament { Brand = "Grilon3", Type = "PLA", PriceKg = 18500 });

        store.Guardar(datos);

        using var documento = JsonDocument.Parse(File.ReadAllText(temp.Archivo()));
        var filamento = documento.RootElement.GetProperty("filaments")[0];

        Assert.Equal("Grilon3", filamento.GetProperty("brand").GetString());
        Assert.Equal("PLA", filamento.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Number, filamento.GetProperty("price_kg").ValueKind);
        Assert.Equal(18500d, filamento.GetProperty("price_kg").GetDouble());
        Assert.Equal(JsonValueKind.String, filamento.GetProperty("id").ValueKind);
        Assert.Equal(datos.Filaments[0].Id, filamento.GetProperty("id").GetString());
    }

    [Fact]
    public void Guardar_SobrescribeYNoDejaArchivoTemporal()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());

        store.Guardar(AppData.PorDefecto());
        var datos = store.Cargar();
        datos.Settings.CostoEnvio = 999;
        store.Guardar(datos);

        Assert.Equal(999d, store.Cargar().Settings.CostoEnvio);
        Assert.Empty(Directory.GetFiles(temp.Ruta, "*.tmp"));
        Assert.Single(Directory.GetFiles(temp.Ruta));
    }

    [Fact]
    public void Guardar_NoEscapaLosAcentos()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());
        var datos = AppData.PorDefecto();
        datos.Settings.NombreNegocio = "Impresión Ñ";

        store.Guardar(datos);

        Assert.Contains("Impresión Ñ", File.ReadAllText(temp.Archivo()), StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RutaVacia_Lanza()
    {
        Assert.Throws<ArgumentException>(() => new ConfigStore("  "));
    }

    [Fact]
    public void Guardar_DatosNulos_Lanza()
    {
        using var temp = new DirectorioTemporal();
        var store = new ConfigStore(temp.Archivo());

        Assert.Throws<ArgumentNullException>(() => store.Guardar(null!));
    }

    private static void AssertSonLosDefaults(AppData datos, bool esperarFilamentosVacios = true)
    {
        Assert.Equal(199.7464d, datos.Settings.PrecioKwh, 10);
        Assert.Equal(150d, datos.Settings.ConsumoW);
        Assert.Equal(5000d, datos.Settings.DesgasteHoras);
        Assert.Equal(305000d, datos.Settings.PrecioRepuestos);
        Assert.Equal(10d, datos.Settings.MargenErrorPct);
        Assert.Equal(21d, datos.Settings.IvaLuzPct);
        Assert.Equal(1.5d, datos.Settings.MargenGanancia);
        Assert.Equal(0d, datos.Settings.CostoEnvio);
        Assert.Equal(CoreConstants.GeometryPorDefecto, datos.Settings.Geometry);
        Assert.Equal(string.Empty, datos.Settings.NombreNegocio);

        if (esperarFilamentosVacios)
        {
            Assert.Empty(datos.Filaments);
        }
    }
}
