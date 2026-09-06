namespace Cotizador3D.App.Services;

/// <summary>
/// Cuadros de dialogo del sistema. Existe para que los ViewModels no toquen
/// tipos de UI (B10: los dialogos siempre tienen ventana padre).
/// </summary>
public interface IDialogService
{
    void MostrarError(string titulo, string mensaje);

    void MostrarInformacion(string titulo, string mensaje);

    bool Confirmar(string titulo, string mensaje);
}
