using Cotizador3D.Core.Models;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class AppDataTests
{
    [Fact]
    public void Filament_NuevoRecibeUnIdNoVacioYUnico()
    {
        var a = new Filament();
        var b = new Filament();

        Assert.False(string.IsNullOrWhiteSpace(a.Id));
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void BuscarFilamento_PorIdString()
    {
        var datos = AppData.PorDefecto();
        datos.Filaments.Add(new Filament { Id = "grilon3_pla_9f3c1a02", Brand = "Grilon3", Type = "PLA", PriceKg = 18500 });
        datos.Filaments.Add(new Filament { Id = "otro", Brand = "Otro", Type = "PETG", PriceKg = 20000 });

        Assert.Equal("Grilon3", datos.BuscarFilamento("grilon3_pla_9f3c1a02")!.Brand);
        Assert.Null(datos.BuscarFilamento("no-existe"));
        Assert.Null(datos.BuscarFilamento(null));
        Assert.Null(datos.BuscarFilamento(""));
    }

    [Fact]
    public void Clonar_EsIndependiente()
    {
        var datos = AppData.PorDefecto();
        datos.Filaments.Add(new Filament { Brand = "A", Type = "PLA", PriceKg = 100 });

        var copia = datos.Clonar();
        copia.Settings.CostoEnvio = 999;
        copia.Filaments[0].Brand = "B";

        Assert.Equal(0d, datos.Settings.CostoEnvio);
        Assert.Equal("A", datos.Filaments[0].Brand);
        Assert.Equal(datos.Filaments[0].Id, copia.Filaments[0].Id);
    }

    [Fact]
    public void DisplayName_UsaElFormatoDelLegado()
    {
        var filamento = new Filament { Brand = "Grilon3", Type = "PLA" };

        Assert.Equal("Grilon3 (PLA)", filamento.DisplayName);
        Assert.Equal("Grilon3 (PLA)", filamento.ToString());
    }
}
