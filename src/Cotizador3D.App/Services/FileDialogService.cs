using Microsoft.Win32;

namespace Cotizador3D.App.Services;

/// <summary>Implementacion sobre <see cref="SaveFileDialog"/>.</summary>
public sealed class FileDialogService : IFileDialogService
{
    public string? PedirRutaPdf(string nombreSugerido)
    {
        var dialogo = new SaveFileDialog
        {
            Title = "Guardar cotización en PDF",
            FileName = nombreSugerido,
            DefaultExt = ".pdf",
            Filter = "Documento PDF (*.pdf)|*.pdf|Todos los archivos (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true,
        };

        var padre = DialogService.VentanaPadre();
        var aceptado = padre is null ? dialogo.ShowDialog() : dialogo.ShowDialog(padre);
        return aceptado == true ? dialogo.FileName : null;
    }
}
