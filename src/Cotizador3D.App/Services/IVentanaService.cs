using Cotizador3D.App.ViewModels;

namespace Cotizador3D.App.Services;

/// <summary>
/// Apertura de las ventanas secundarias. Los ViewModels solo conocen esta
/// interfaz, nunca los tipos de ventana.
/// </summary>
public interface IVentanaService
{
    /// <summary>Abre el gestor de filamentos de forma modal.</summary>
    void MostrarGestorDeFilamentos(FilamentManagerViewModel vm);

    /// <summary>Abre el editor de un filamento. Devuelve true si se guardo.</summary>
    bool MostrarEditorDeFilamento(FilamentEditorViewModel vm);

    /// <summary>Pide los datos del cliente antes de exportar. Devuelve true si se confirmo.</summary>
    bool MostrarDialogoExportacion(ExportPdfViewModel vm);
}
