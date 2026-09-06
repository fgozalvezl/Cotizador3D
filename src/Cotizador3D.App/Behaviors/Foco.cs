using System.Windows;
using System.Windows.Input;

namespace Cotizador3D.App.Behaviors;

/// <summary>
/// Ejecuta un comando cuando el foco de teclado sale de un elemento (o de
/// cualquiera de sus hijos). Reemplaza al autoguardado por <c>&lt;FocusOut&gt;</c>
/// del legado (docs/SPEC-legacy.md 3.1.4) sin poner logica en el code-behind.
/// </summary>
public static class Foco
{
    public static readonly DependencyProperty ComandoAlPerderFocoProperty =
        DependencyProperty.RegisterAttached(
            "ComandoAlPerderFoco",
            typeof(ICommand),
            typeof(Foco),
            new PropertyMetadata(null, AlCambiarComando));

    public static ICommand? GetComandoAlPerderFoco(DependencyObject elemento)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        return (ICommand?)elemento.GetValue(ComandoAlPerderFocoProperty);
    }

    public static void SetComandoAlPerderFoco(DependencyObject elemento, ICommand? valor)
    {
        ArgumentNullException.ThrowIfNull(elemento);
        elemento.SetValue(ComandoAlPerderFocoProperty, valor);
    }

    private static void AlCambiarComando(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement elemento)
        {
            return;
        }

        elemento.LostKeyboardFocus -= AlPerderFoco;
        if (e.NewValue is ICommand)
        {
            elemento.LostKeyboardFocus += AlPerderFoco;
        }
    }

    private static void AlPerderFoco(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is DependencyObject elemento
            && GetComandoAlPerderFoco(elemento) is { } comando
            && comando.CanExecute(null))
        {
            comando.Execute(null);
        }
    }
}
