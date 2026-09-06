using Cotizador3D.Core.Formatting;
using Cotizador3D.Core.Models;

namespace Cotizador3D.App.ViewModels;

/// <summary>
/// Fila de la lista del gestor: "Marca (Tipo) - $1.234,56/kg"
/// (docs/SPEC-legacy.md 3.2, con el formato es-AR de B9).
/// </summary>
public sealed class FilamentoItemViewModel
{
    public FilamentoItemViewModel(Filament modelo)
    {
        Modelo = modelo ?? throw new ArgumentNullException(nameof(modelo));
    }

    public Filament Modelo { get; }

    public string Texto => $"{Modelo.DisplayName} - {MoneyFormat.MonedaPorKg(Modelo.PriceKg)}";
}
