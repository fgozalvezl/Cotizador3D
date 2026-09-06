using System.Linq;
using System.Windows;

namespace Cotizador3D.App.Services;

/// <summary>Implementacion sobre <see cref="MessageBox"/>, siempre con padre (B10).</summary>
public sealed class DialogService : IDialogService
{
    public void MostrarError(string titulo, string mensaje) =>
        Mostrar(titulo, mensaje, MessageBoxButton.OK, MessageBoxImage.Error);

    public void MostrarInformacion(string titulo, string mensaje) =>
        Mostrar(titulo, mensaje, MessageBoxButton.OK, MessageBoxImage.Information);

    public bool Confirmar(string titulo, string mensaje) =>
        Mostrar(titulo, mensaje, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    /// <summary>Ventana activa (o la principal) para que el dialogo nunca quede detras.</summary>
    internal static Window? VentanaPadre()
    {
        var app = Application.Current;
        if (app is null)
        {
            return null;
        }

        return app.Windows.OfType<Window>().FirstOrDefault(v => v.IsActive) ?? app.MainWindow;
    }

    private static MessageBoxResult Mostrar(string titulo, string mensaje, MessageBoxButton botones, MessageBoxImage icono)
    {
        var padre = VentanaPadre();
        return padre is null
            ? MessageBox.Show(mensaje, titulo, botones, icono)
            : MessageBox.Show(padre, mensaje, titulo, botones, icono);
    }
}
