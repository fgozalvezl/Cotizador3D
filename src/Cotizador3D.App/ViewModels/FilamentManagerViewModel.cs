using System.Collections.ObjectModel;
using Cotizador3D.App.Mvvm;
using Cotizador3D.App.Services;
using Cotizador3D.Core.Models;

namespace Cotizador3D.App.ViewModels;

/// <summary>
/// Gestor de filamentos (docs/SPEC-legacy.md 3.2). Cada alta, edicion o baja
/// se persiste enseguida y refresca el combo de la ventana principal.
/// </summary>
public sealed class FilamentManagerViewModel : ObservableObject
{
    private readonly AppData _datos;
    private readonly IDialogService _dialogos;
    private readonly IVentanaService _ventanas;
    private readonly Action _alCambiar;
    private FilamentoItemViewModel? _seleccionado;

    public FilamentManagerViewModel(
        AppData datos,
        IDialogService dialogos,
        IVentanaService ventanas,
        Action alCambiar)
    {
        _datos = datos ?? throw new ArgumentNullException(nameof(datos));
        _dialogos = dialogos ?? throw new ArgumentNullException(nameof(dialogos));
        _ventanas = ventanas ?? throw new ArgumentNullException(nameof(ventanas));
        _alCambiar = alCambiar ?? throw new ArgumentNullException(nameof(alCambiar));

        Filamentos = new ObservableCollection<FilamentoItemViewModel>();
        AgregarCommand = new RelayCommand(Agregar);
        EditarCommand = new RelayCommand(Editar, () => Seleccionado is not null);
        EliminarCommand = new RelayCommand(Eliminar, () => Seleccionado is not null);

        Refrescar(null);
    }

    public ObservableCollection<FilamentoItemViewModel> Filamentos { get; }

    public FilamentoItemViewModel? Seleccionado
    {
        get => _seleccionado;
        set
        {
            if (SetProperty(ref _seleccionado, value))
            {
                OnPropertyChanged(nameof(HaySeleccion));
                EditarCommand.NotificarCambioDeEstado();
                EliminarCommand.NotificarCambioDeEstado();
            }
        }
    }

    public bool HaySeleccion => Seleccionado is not null;

    public bool ListaVacia => Filamentos.Count == 0;

    public RelayCommand AgregarCommand { get; }

    public RelayCommand EditarCommand { get; }

    public RelayCommand EliminarCommand { get; }

    private void Agregar()
    {
        var editor = new FilamentEditorViewModel(_dialogos);
        if (!_ventanas.MostrarEditorDeFilamento(editor) || editor.Resultado is null)
        {
            return;
        }

        _datos.Filaments.Add(editor.Resultado);
        _alCambiar();
        Refrescar(editor.Resultado.Id);
    }

    private void Editar()
    {
        if (Seleccionado is null)
        {
            return;
        }

        var original = Seleccionado.Modelo;
        var editor = new FilamentEditorViewModel(_dialogos, original.Clonar());
        if (!_ventanas.MostrarEditorDeFilamento(editor) || editor.Resultado is null)
        {
            return;
        }

        var indice = _datos.Filaments.FindIndex(f => string.Equals(f.Id, original.Id, StringComparison.Ordinal));
        if (indice < 0)
        {
            return;
        }

        // Se conserva el id original y se reemplazan los datos editables.
        _datos.Filaments[indice].Brand = editor.Resultado.Brand;
        _datos.Filaments[indice].Type = editor.Resultado.Type;
        _datos.Filaments[indice].PriceKg = editor.Resultado.PriceKg;

        _alCambiar();
        Refrescar(original.Id);
    }

    private void Eliminar()
    {
        if (Seleccionado is null)
        {
            return;
        }

        var filamento = Seleccionado.Modelo;
        var confirmado = _dialogos.Confirmar(
            "Confirmar",
            $"¿Seguro que querés eliminar el filamento {filamento.DisplayName}?");

        if (!confirmado)
        {
            return;
        }

        _datos.Filaments.RemoveAll(f => string.Equals(f.Id, filamento.Id, StringComparison.Ordinal));
        _alCambiar();
        Refrescar(null);
    }

    private void Refrescar(string? idSeleccionado)
    {
        Filamentos.Clear();
        foreach (var filamento in _datos.Filaments)
        {
            Filamentos.Add(new FilamentoItemViewModel(filamento));
        }

        Seleccionado = idSeleccionado is null
            ? null
            : Filamentos.FirstOrDefault(i => string.Equals(i.Modelo.Id, idSeleccionado, StringComparison.Ordinal));

        OnPropertyChanged(nameof(ListaVacia));
    }
}
