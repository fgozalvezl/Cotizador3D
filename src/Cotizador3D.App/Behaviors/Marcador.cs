using System.Windows;

namespace Cotizador3D.App.Behaviors;

/// <summary>
/// Texto de ayuda ("placeholder") para los cuadros de texto, que WPF no trae
/// de fabrica. La plantilla del TextBox lo muestra cuando el campo esta vacio.
/// </summary>
public static class Marcador
{
    public static readonly DependencyProperty TextoProperty =
        DependencyProperty.RegisterAttached(
            "Texto",
            typeof(string),
            typeof(Marcador),
            new PropertyMetadata(string.Empty));

    public static string GetTexto(DependencyObject elemento)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        return (string)elemento.GetValue(TextoProperty);
    }

    public static void SetTexto(DependencyObject elemento, string valor)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        elemento.SetValue(TextoProperty, valor);
    }
}
