using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cotizador3D.App.Mvvm;

/// <summary>
/// Base minima de MVVM: notifica cambios de propiedad sin depender de ningun
/// framework externo.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Notifica el cambio de una propiedad (por defecto, la que llama).</summary>
    protected void OnPropertyChanged([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));

    /// <summary>Asigna el campo y notifica solo si el valor cambio.</summary>
    protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string? nombre = null)
    {
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return false;
        }

        campo = valor;
        OnPropertyChanged(nombre);
        return true;
    }
}
