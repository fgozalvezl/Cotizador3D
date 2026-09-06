using System.Windows;
using Cotizador3D.App.ViewModels;

namespace Cotizador3D.App.Views;

/// <summary>Pide cliente y trabajo antes de generar el PDF.</summary>
public partial class ExportPdfDialog : Window
{
    public ExportPdfDialog(ExportPdfViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        InitializeComponent();

        DataContext = vm;
        vm.SolicitudCerrar += AlSolicitarCierre;
    }

    private void AlSolicitarCierre(object? remitente, bool confirmado)
    {
        if (IsLoaded)
        {
            DialogResult = confirmado;
        }
    }
}
