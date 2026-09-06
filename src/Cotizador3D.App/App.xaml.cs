using System.Windows;
using Cotizador3D.App.Services;
using Cotizador3D.App.ViewModels;
using Cotizador3D.Core.Persistence;

namespace Cotizador3D.App;

/// <summary>
/// Punto de entrada: arma los servicios, el ViewModel principal y la ventana.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var almacen = new ConfigStore();
        var dialogos = new DialogService();
        var archivos = new FileDialogService();
        var ventanas = new VentanaService();
        var pdf = new PdfExportService();

        var vm = new MainViewModel(almacen, dialogos, archivos, ventanas, pdf);
        var ventana = new MainWindow(vm);

        this.MainWindow = ventana;
        ventana.Show();
    }
}
