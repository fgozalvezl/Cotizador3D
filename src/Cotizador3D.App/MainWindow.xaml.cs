using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using Cotizador3D.App.Services;
using Cotizador3D.App.ViewModels;

namespace Cotizador3D.App;

/// <summary>
/// Ventana principal. El code-behind se limita a lo que no se puede declarar:
/// geometria de la ventana y disparo de la animacion de resultados.
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));

        InitializeComponent();

        DataContext = _vm;
        _vm.CalculoExitoso += AlCalcularConExito;

        AplicarGeometria(_vm.ObtenerGeometria());
    }

    /// <summary>Cada calculo exitoso reanima el panel de resultados.</summary>
    private void AlCalcularConExito(object? remitente, EventArgs e)
    {
        if (TryFindResource("AnimEntradaPanel") is Storyboard animacion)
        {
            animacion.Begin(PanelResultados);
        }
    }

    /// <summary>
    /// Restaura tamano y posicion guardados (docs/SPEC-legacy.md 4.1), sin
    /// dejar la ventana fuera del area visible.
    /// </summary>
    private void AplicarGeometria(GeometriaVentana geometria)
    {
        Width = Math.Max(geometria.Ancho, MinWidth);
        Height = Math.Max(geometria.Alto, MinHeight);

        if (!geometria.TienePosicion)
        {
            return;
        }

        double x = geometria.Izquierda!.Value;
        double y = geometria.Arriba!.Value;

        var izquierda = SystemParameters.VirtualScreenLeft;
        var arriba = SystemParameters.VirtualScreenTop;
        var derecha = izquierda + SystemParameters.VirtualScreenWidth;
        var abajo = arriba + SystemParameters.VirtualScreenHeight;

        // Se exige que quede visible al menos una franja de la ventana.
        if (x + 120 < izquierda || x > derecha - 120 || y < arriba - 10 || y > abajo - 80)
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = x;
        Top = y;
    }

    /// <summary>La geometria se guarda SIEMPRE al cerrar (B8).</summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        var limites = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;

        var ancho = double.IsNaN(limites.Width) || limites.Width <= 0 ? ActualWidth : limites.Width;
        var alto = double.IsNaN(limites.Height) || limites.Height <= 0 ? ActualHeight : limites.Height;
        var x = double.IsNaN(limites.X) ? 0 : limites.X;
        var y = double.IsNaN(limites.Y) ? 0 : limites.Y;

        _vm.GuardarAlCerrar(new GeometriaVentana(
            (int)Math.Round(ancho),
            (int)Math.Round(alto),
            (int)Math.Round(x),
            (int)Math.Round(y)));
    }
}
