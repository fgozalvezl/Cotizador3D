using System.Windows;
using Cotizador3D.App.ViewModels;

namespace Cotizador3D.App.Views;

/// <summary>Editor de un filamento (docs/SPEC-legacy.md 3.3).</summary>
public partial class FilamentEditorWindow : Window
{
    public FilamentEditorWindow(FilamentEditorViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        InitializeComponent();

        DataContext = vm;
        vm.SolicitudCerrar += AlSolicitarCierre;
    }

    private void AlSolicitarCierre(object? remitente, bool guardado)
    {
        if (IsLoaded)
        {
            DialogResult = guardado;
        }
    }
}
