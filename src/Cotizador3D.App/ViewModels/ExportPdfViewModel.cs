using Cotizador3D.App.Mvvm;

namespace Cotizador3D.App.ViewModels;

/// <summary>Datos opcionales que se piden antes de generar el PDF del cliente.</summary>
public sealed class ExportPdfViewModel : ObservableObject
{
    private string _cliente = string.Empty;
    private string _trabajo = string.Empty;

    public ExportPdfViewModel()
    {
        ConfirmarCommand = new RelayCommand(() => SolicitudCerrar?.Invoke(this, true));
        CancelarCommand = new RelayCommand(() => SolicitudCerrar?.Invoke(this, false));
    }

    /// <summary>Se pide cerrar la ventana; el bool indica si se confirmo.</summary>
    public event EventHandler<bool>? SolicitudCerrar;

    public string Cliente
    {
        get => _cliente;
        set => SetProperty(ref _cliente, value);
    }

    public string Trabajo
    {
        get => _trabajo;
        set => SetProperty(ref _trabajo, value);
    }

    public RelayCommand ConfirmarCommand { get; }

    public RelayCommand CancelarCommand { get; }
}
