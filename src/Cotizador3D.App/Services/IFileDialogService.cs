namespace Cotizador3D.App.Services;

/// <summary>Dialogo de archivo para guardar el PDF de la cotizacion.</summary>
public interface IFileDialogService
{
    /// <summary>Devuelve la ruta elegida, o null si el usuario cancelo.</summary>
    string? PedirRutaPdf(string nombreSugerido);
}
