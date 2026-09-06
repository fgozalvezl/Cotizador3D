using System.Windows;
using Cotizador3D.App.ViewModels;
using Cotizador3D.App.Views;

namespace Cotizador3D.App.Services;

/// <summary>Implementacion real: crea las ventanas WPF y las muestra modales.</summary>
public sealed class VentanaService : IVentanaService
{
    public void MostrarGestorDeFilamentos(FilamentManagerViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        var ventana = new FilamentManagerWindow { DataContext = vm };
        MostrarModal(ventana);
    }

    public bool MostrarEditorDeFilamento(FilamentEditorViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        var ventana = new FilamentEditorWindow(vm);
        return MostrarModal(ventana) == true;
    }

    public bool MostrarDialogoExportacion(ExportPdfViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);
        var ventana = new ExportPdfDialog(vm);
        return MostrarModal(ventana) == true;
    }

    private static bool? MostrarModal(Window ventana)
    {
        var padre = DialogService.VentanaPadre();
        if (padre is not null && !ReferenceEquals(padre, ventana))
        {
            ventana.Owner = padre;
            ventana.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        return ventana.ShowDialog();
    }
}
