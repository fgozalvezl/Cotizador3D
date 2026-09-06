using Cotizador3D.App.Mvvm;
using Cotizador3D.App.Services;
using Cotizador3D.Core;
using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Models;

namespace Cotizador3D.App.ViewModels;

/// <summary>
/// Editor de un filamento (alta o edicion), docs/SPEC-legacy.md 3.3.
/// El combo de tipo NO es editable (B16): solo valores de
/// <see cref="CoreConstants.TiposDeFilamento"/>.
/// </summary>
public sealed class FilamentEditorViewModel : ObservableObject
{
    private readonly IDialogService _dialogos;
    private readonly Filament? _original;
    private string _marca = string.Empty;
    private string _tipoSeleccionado;
    private string _precioTexto = string.Empty;

    public FilamentEditorViewModel(IDialogService dialogos, Filament? filamento = null)
    {
        _dialogos = dialogos ?? throw new ArgumentNullException(nameof(dialogos));
        _original = filamento;

        _tipoSeleccionado = CoreConstants.TiposDeFilamento[0];

        if (filamento is not null)
        {
            _marca = filamento.Brand;
            if (!string.IsNullOrWhiteSpace(filamento.Type) && CoreConstants.TiposDeFilamento.Contains(filamento.Type))
            {
                _tipoSeleccionado = filamento.Type;
            }

            _precioTexto = TextoNumerico.Formatear(filamento.PriceKg);
        }

        GuardarCommand = new RelayCommand(Guardar);
        CancelarCommand = new RelayCommand(() => SolicitudCerrar?.Invoke(this, false));
    }

    /// <summary>Se pide cerrar la ventana; el bool indica si se guardo.</summary>
    public event EventHandler<bool>? SolicitudCerrar;

    public string Titulo => _original is null ? "Agregar Filamento" : "Editar Filamento";

    public IReadOnlyList<string> Tipos => CoreConstants.TiposDeFilamento;

    public string Marca
    {
        get => _marca;
        set => SetProperty(ref _marca, value);
    }

    public string TipoSeleccionado
    {
        get => _tipoSeleccionado;
        set => SetProperty(ref _tipoSeleccionado, value);
    }

    public string PrecioTexto
    {
        get => _precioTexto;
        set => SetProperty(ref _precioTexto, value);
    }

    /// <summary>Filamento resultante, disponible solo si se guardo.</summary>
    public Filament? Resultado { get; private set; }

    public RelayCommand GuardarCommand { get; }

    public RelayCommand CancelarCommand { get; }

    private void Guardar()
    {
        var marca = Marca.Trim();
        var tipo = (TipoSeleccionado ?? string.Empty).Trim();
        var precioTexto = (PrecioTexto ?? string.Empty).Trim();

        if (marca.Length == 0 || tipo.Length == 0 || precioTexto.Length == 0)
        {
            _dialogos.MostrarError("Error", "Todos los campos son obligatorios.");
            return;
        }

        if (!NumberParser.TryParse(precioTexto, out var precio))
        {
            _dialogos.MostrarError("Error", "El precio debe ser un número válido.");
            return;
        }

        if (precio < 0)
        {
            _dialogos.MostrarError("Error", "El precio por kilo no puede ser negativo.");
            return;
        }

        Resultado = new Filament
        {
            Id = _original?.Id ?? Filament.NuevoId(),
            Brand = marca,
            Type = tipo,
            PriceKg = precio,
        };

        SolicitudCerrar?.Invoke(this, true);
    }
}
