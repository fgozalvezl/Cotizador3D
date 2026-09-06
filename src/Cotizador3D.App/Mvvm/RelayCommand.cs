using System.Windows.Input;

namespace Cotizador3D.App.Mvvm;

/// <summary>
/// Comando sencillo basado en delegados. No usa <c>CommandManager</c>: el
/// ViewModel avisa explicitamente con <see cref="NotificarCambioDeEstado"/>.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _ejecutar;
    private readonly Func<bool>? _puedeEjecutar;

    public RelayCommand(Action ejecutar, Func<bool>? puedeEjecutar = null)
    {
        _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));
        _puedeEjecutar = puedeEjecutar;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _puedeEjecutar is null || _puedeEjecutar();

    public void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            _ejecutar();
        }
    }

    /// <summary>Fuerza a la UI a volver a consultar <see cref="CanExecute"/>.</summary>
    public void NotificarCambioDeEstado() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
