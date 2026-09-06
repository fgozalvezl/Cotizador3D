using System.Collections.ObjectModel;
using System.Globalization;
using Cotizador3D.App.Mvvm;
using Cotizador3D.App.Services;
using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Formatting;
using Cotizador3D.Core.Models;
using Cotizador3D.Core.Persistence;

namespace Cotizador3D.App.ViewModels;

/// <summary>
/// ViewModel de la ventana principal: parametros fijos, datos de la impresion,
/// resultados y exportacion (docs/SPEC-legacy.md 3.1).
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private const string ValorCero = "$ 0,00";

    private readonly ConfigStore _almacen;
    private readonly IDialogService _dialogos;
    private readonly IFileDialogService _archivos;
    private readonly IVentanaService _ventanas;
    private readonly IPdfExportService _pdf;
    private readonly AppData _datos;

    private string _precioKwhTexto;
    private string _consumoWTexto;
    private string _desgasteHorasTexto;
    private string _precioRepuestosTexto;
    private string _margenErrorTexto;
    private string _ivaLuzTexto;
    private string _margenGananciaTexto;
    private string _costoEnvioTexto;
    private string _nombreNegocio;

    private string _gramosTexto = string.Empty;
    private string _diasTexto = string.Empty;
    private string _horasTexto = string.Empty;
    private string _minutosTexto = string.Empty;
    private string _segundosTexto = string.Empty;

    private string? _filamentoSeleccionadoId;

    private string _materialTexto = ValorCero;
    private string _luzTexto = ValorCero;
    private string _desgasteTexto = ValorCero;
    private string _margenErrorValorTexto = ValorCero;
    private string _ivaLuzValorTexto = ValorCero;
    private string _costoTotalTexto = ValorCero;
    private string _precioVentaTexto = ValorCero;
    private string _costoEnvioResultadoTexto = ValorCero;
    private string _precioFinalTexto = ValorCero;
    private string _tiempoCalculadoTexto = string.Empty;

    private bool _mostrarEnvio;
    private bool _hayError;
    private string _mensajeError = string.Empty;
    private bool _hayAdvertencia;
    private string _mensajeAdvertencia = string.Empty;
    private bool _puedeExportar;

    private QuoteResult? _ultimoResultado;
    private Filament? _filamentoDelCalculo;
    private double _gramosDelCalculo;

    public MainViewModel(
        ConfigStore almacen,
        IDialogService dialogos,
        IFileDialogService archivos,
        IVentanaService ventanas,
        IPdfExportService pdf)
    {
        _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        _dialogos = dialogos ?? throw new ArgumentNullException(nameof(dialogos));
        _archivos = archivos ?? throw new ArgumentNullException(nameof(archivos));
        _ventanas = ventanas ?? throw new ArgumentNullException(nameof(ventanas));
        _pdf = pdf ?? throw new ArgumentNullException(nameof(pdf));

        _datos = _almacen.Cargar();

        var s = _datos.Settings;
        _precioKwhTexto = TextoNumerico.Formatear(s.PrecioKwh);
        _consumoWTexto = TextoNumerico.Formatear(s.ConsumoW);
        _desgasteHorasTexto = TextoNumerico.Formatear(s.DesgasteHoras);
        _precioRepuestosTexto = TextoNumerico.Formatear(s.PrecioRepuestos);
        _margenErrorTexto = TextoNumerico.Formatear(s.MargenErrorPct);
        _ivaLuzTexto = TextoNumerico.Formatear(s.IvaLuzPct);
        _margenGananciaTexto = TextoNumerico.Formatear(s.MargenGanancia);
        _costoEnvioTexto = TextoNumerico.Formatear(s.CostoEnvio);
        _nombreNegocio = s.NombreNegocio;

        Filamentos = new ObservableCollection<Filament>();

        CalcularCommand = new RelayCommand(Calcular);
        GestionarFilamentosCommand = new RelayCommand(GestionarFilamentos);
        ExportarPdfCommand = new RelayCommand(ExportarPdf, () => PuedeExportar);
        GuardarConfiguracionCommand = new RelayCommand(GuardarConfiguracion);
        DescartarErrorCommand = new RelayCommand(() => HayError = false);
        DescartarAdvertenciaCommand = new RelayCommand(() => HayAdvertencia = false);

        RefrescarFilamentos(null);
    }

    /// <summary>Se dispara tras cada calculo exitoso (la vista anima el panel).</summary>
    public event EventHandler? CalculoExitoso;

    // ---------------------------------------------------------------- Parametros fijos

    public string PrecioKwhTexto
    {
        get => _precioKwhTexto;
        set
        {
            if (SetProperty(ref _precioKwhTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    public string ConsumoWTexto
    {
        get => _consumoWTexto;
        set
        {
            if (SetProperty(ref _consumoWTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    public string DesgasteHorasTexto
    {
        get => _desgasteHorasTexto;
        set
        {
            if (SetProperty(ref _desgasteHorasTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    public string PrecioRepuestosTexto
    {
        get => _precioRepuestosTexto;
        set
        {
            if (SetProperty(ref _precioRepuestosTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    public string MargenErrorTexto
    {
        get => _margenErrorTexto;
        set
        {
            if (SetProperty(ref _margenErrorTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    /// <summary>IVA de la energia, ahora editable (B14/B15).</summary>
    public string IvaLuzTexto
    {
        get => _ivaLuzTexto;
        set
        {
            if (SetProperty(ref _ivaLuzTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    public string MargenGananciaTexto
    {
        get => _margenGananciaTexto;
        set
        {
            if (SetProperty(ref _margenGananciaTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    public string CostoEnvioTexto
    {
        get => _costoEnvioTexto;
        set
        {
            if (SetProperty(ref _costoEnvioTexto, value))
            {
                AplicarAjustes();
            }
        }
    }

    /// <summary>Nombre del negocio que encabeza el PDF del cliente.</summary>
    public string NombreNegocio
    {
        get => _nombreNegocio;
        set
        {
            if (SetProperty(ref _nombreNegocio, value))
            {
                AplicarAjustes();
            }
        }
    }

    // ---------------------------------------------------------------- Datos de la impresion

    public ObservableCollection<Filament> Filamentos { get; }

    public string? FilamentoSeleccionadoId
    {
        get => _filamentoSeleccionadoId;
        set => SetProperty(ref _filamentoSeleccionadoId, value);
    }

    public bool SinFilamentos => Filamentos.Count == 0;

    public string GramosTexto
    {
        get => _gramosTexto;
        set => SetProperty(ref _gramosTexto, value);
    }

    public string DiasTexto
    {
        get => _diasTexto;
        set => SetProperty(ref _diasTexto, value);
    }

    public string HorasTexto
    {
        get => _horasTexto;
        set => SetProperty(ref _horasTexto, value);
    }

    public string MinutosTexto
    {
        get => _minutosTexto;
        set => SetProperty(ref _minutosTexto, value);
    }

    public string SegundosTexto
    {
        get => _segundosTexto;
        set => SetProperty(ref _segundosTexto, value);
    }

    // ---------------------------------------------------------------- Resultados

    public string MaterialTexto
    {
        get => _materialTexto;
        private set => SetProperty(ref _materialTexto, value);
    }

    public string LuzTexto
    {
        get => _luzTexto;
        private set => SetProperty(ref _luzTexto, value);
    }

    public string DesgasteTexto
    {
        get => _desgasteTexto;
        private set => SetProperty(ref _desgasteTexto, value);
    }

    public string MargenErrorValorTexto
    {
        get => _margenErrorValorTexto;
        private set => SetProperty(ref _margenErrorValorTexto, value);
    }

    public string IvaLuzValorTexto
    {
        get => _ivaLuzValorTexto;
        private set => SetProperty(ref _ivaLuzValorTexto, value);
    }

    public string CostoTotalTexto
    {
        get => _costoTotalTexto;
        private set => SetProperty(ref _costoTotalTexto, value);
    }

    public string PrecioVentaTexto
    {
        get => _precioVentaTexto;
        private set => SetProperty(ref _precioVentaTexto, value);
    }

    public string CostoEnvioResultadoTexto
    {
        get => _costoEnvioResultadoTexto;
        private set => SetProperty(ref _costoEnvioResultadoTexto, value);
    }

    public string PrecioFinalTexto
    {
        get => _precioFinalTexto;
        private set => SetProperty(ref _precioFinalTexto, value);
    }

    /// <summary>Tiempo de impresion del ultimo calculo, en formato legible.</summary>
    public string TiempoCalculadoTexto
    {
        get => _tiempoCalculadoTexto;
        private set => SetProperty(ref _tiempoCalculadoTexto, value);
    }

    /// <summary>Etiqueta viva del IVA: "IVA Luz (21%):" (B14).</summary>
    public string EtiquetaIvaLuz =>
        $"IVA Luz ({MoneyFormat.Porcentaje(_datos.Settings.IvaLuzPct)}%):";

    public bool MostrarEnvio
    {
        get => _mostrarEnvio;
        private set => SetProperty(ref _mostrarEnvio, value);
    }

    public bool HayError
    {
        get => _hayError;
        private set => SetProperty(ref _hayError, value);
    }

    public string MensajeError
    {
        get => _mensajeError;
        private set => SetProperty(ref _mensajeError, value);
    }

    public bool HayAdvertencia
    {
        get => _hayAdvertencia;
        private set => SetProperty(ref _hayAdvertencia, value);
    }

    public string MensajeAdvertencia
    {
        get => _mensajeAdvertencia;
        private set => SetProperty(ref _mensajeAdvertencia, value);
    }

    /// <summary>Solo se puede exportar despues de un calculo exitoso.</summary>
    public bool PuedeExportar
    {
        get => _puedeExportar;
        private set
        {
            if (SetProperty(ref _puedeExportar, value))
            {
                ExportarPdfCommand.NotificarCambioDeEstado();
            }
        }
    }

    // ---------------------------------------------------------------- Comandos

    public RelayCommand CalcularCommand { get; }

    public RelayCommand GestionarFilamentosCommand { get; }

    public RelayCommand ExportarPdfCommand { get; }

    public RelayCommand GuardarConfiguracionCommand { get; }

    public RelayCommand DescartarErrorCommand { get; }

    public RelayCommand DescartarAdvertenciaCommand { get; }

    // ---------------------------------------------------------------- Geometria

    /// <summary>Geometria guardada, para que la vista posicione la ventana.</summary>
    public GeometriaVentana ObtenerGeometria() => GeometriaVentana.Parsear(_datos.Settings.Geometry);

    /// <summary>
    /// Guardado al cerrar: la geometria SIEMPRE se persiste, y con ella los
    /// ajustes que sean validos (B8).
    /// </summary>
    public void GuardarAlCerrar(GeometriaVentana geometria)
    {
        ArgumentNullException.ThrowIfNull(geometria);
        AplicarAjustes();
        _datos.Settings.Geometry = geometria.Formatear();
        Persistir();
    }

    /// <summary>Autoguardado: se llama al perder el foco un campo de ajustes.</summary>
    public void GuardarConfiguracion()
    {
        AplicarAjustes();
        Persistir();
    }

    // ---------------------------------------------------------------- Logica

    private Filament? FilamentoSeleccionado => _datos.BuscarFilamento(FilamentoSeleccionadoId);

    /// <summary>
    /// Vuelca a la configuracion los campos que parsean bien; los invalidos se
    /// ignoran (B8: se guarda lo valido, sin cancelar todo).
    /// </summary>
    private void AplicarAjustes()
    {
        var s = _datos.Settings;

        if (NumberParser.TryParse(PrecioKwhTexto, out var kwh, AppSettings.PrecioKwhPorDefecto) && kwh >= 0)
        {
            s.PrecioKwh = kwh;
        }

        if (NumberParser.TryParse(ConsumoWTexto, out var consumo, AppSettings.ConsumoWPorDefecto) && consumo >= 0)
        {
            s.ConsumoW = consumo;
        }

        if (NumberParser.TryParse(DesgasteHorasTexto, out var desgaste, AppSettings.DesgasteHorasPorDefecto) && desgaste >= 0)
        {
            s.DesgasteHoras = desgaste;
        }

        if (NumberParser.TryParse(PrecioRepuestosTexto, out var repuestos, AppSettings.PrecioRepuestosPorDefecto) && repuestos >= 0)
        {
            s.PrecioRepuestos = repuestos;
        }

        if (NumberParser.TryParse(MargenErrorTexto, out var margenError, AppSettings.MargenErrorPctPorDefecto) && margenError >= 0)
        {
            s.MargenErrorPct = margenError;
        }

        if (NumberParser.TryParse(IvaLuzTexto, out var iva, AppSettings.IvaLuzPctPorDefecto) && iva >= 0)
        {
            s.IvaLuzPct = iva;
        }

        if (NumberParser.TryParse(MargenGananciaTexto, out var ganancia, AppSettings.MargenGananciaPorDefecto) && ganancia >= 0)
        {
            s.MargenGanancia = ganancia;
        }

        if (NumberParser.TryParse(CostoEnvioTexto, out var envio, AppSettings.CostoEnvioPorDefecto) && envio >= 0)
        {
            s.CostoEnvio = envio;
        }

        s.NombreNegocio = NombreNegocio ?? string.Empty;

        OnPropertyChanged(nameof(EtiquetaIvaLuz));
    }

    private void Persistir()
    {
        try
        {
            _almacen.Guardar(_datos);
        }
        catch (Exception ex)
        {
            // B11: un fallo de guardado tiene que verse.
            MostrarError($"No se pudo guardar la configuración en {_almacen.RutaArchivo}. Detalle: {ex.Message}");
        }
    }

    private void Calcular()
    {
        HayError = false;
        HayAdvertencia = false;

        try
        {
            var settings = LeerSettingsDeLaUi();
            _datos.Settings = settings;
            OnPropertyChanged(nameof(EtiquetaIvaLuz));

            var entrada = new QuoteInput
            {
                Filamento = FilamentoSeleccionado,
                Gramos = NumberParser.Parse(GramosTexto, 0, "Gramos de Filamento"),
                Dias = NumberParser.Parse(DiasTexto, 0, "Días"),
                Horas = NumberParser.Parse(HorasTexto, 0, "Horas"),
                Minutos = NumberParser.Parse(MinutosTexto, 0, "Min"),
                Segundos = NumberParser.Parse(SegundosTexto, 0, "Seg"),
                Settings = settings,
            };

            var resultado = QuoteCalculator.Calcular(entrada);

            MostrarResultado(resultado, entrada);

            if (QuoteCalculator.DesgasteAnulado(settings))
            {
                // B7: el calculo sigue, pero el usuario tiene que enterarse.
                MostrarAdvertencia(
                    "La vida útil de la máquina es 0, así que la cotización no incluye desgaste. " +
                    "Cargá las horas de vida útil para amortizar la máquina.");
            }
        }
        catch (QuoteValidationException ex)
        {
            // B5: los resultados viejos no pueden quedar en pantalla.
            LimpiarResultados();
            MostrarError(ex.Message);
        }
        catch (Exception ex)
        {
            LimpiarResultados();
            MostrarError($"Ocurrió un error inesperado: {ex.Message}");
        }
    }

    /// <summary>
    /// Lee los 8 parametros de la UI de forma estricta: un texto invalido
    /// aborta el calculo con un mensaje claro.
    /// </summary>
    private AppSettings LeerSettingsDeLaUi()
    {
        var s = _datos.Settings.Clonar();
        s.PrecioKwh = NumberParser.Parse(PrecioKwhTexto, AppSettings.PrecioKwhPorDefecto, "Precio Kwh");
        s.ConsumoW = NumberParser.Parse(ConsumoWTexto, AppSettings.ConsumoWPorDefecto, "Consumo real por hora (W)");
        s.DesgasteHoras = NumberParser.Parse(DesgasteHorasTexto, AppSettings.DesgasteHorasPorDefecto, "Vida útil de la Máquina (horas)");
        s.PrecioRepuestos = NumberParser.Parse(PrecioRepuestosTexto, AppSettings.PrecioRepuestosPorDefecto, "Costo Repuestos");
        s.MargenErrorPct = NumberParser.Parse(MargenErrorTexto, AppSettings.MargenErrorPctPorDefecto, "% de Margen de error");
        s.IvaLuzPct = NumberParser.Parse(IvaLuzTexto, AppSettings.IvaLuzPctPorDefecto, "% de IVA Luz");
        s.MargenGanancia = NumberParser.Parse(MargenGananciaTexto, AppSettings.MargenGananciaPorDefecto, "Margen de Ganancia (x)");
        s.CostoEnvio = NumberParser.Parse(CostoEnvioTexto, AppSettings.CostoEnvioPorDefecto, "Costo de Envío");
        s.NombreNegocio = NombreNegocio ?? string.Empty;
        return s;
    }

    private void MostrarResultado(QuoteResult resultado, QuoteInput entrada)
    {
        MaterialTexto = MoneyFormat.Moneda(resultado.PrecioMaterial);
        LuzTexto = MoneyFormat.Moneda(resultado.PrecioLuz);
        DesgasteTexto = MoneyFormat.Moneda(resultado.DesgasteMaquina);
        MargenErrorValorTexto = MoneyFormat.Moneda(resultado.MargenErrorValor);
        IvaLuzValorTexto = MoneyFormat.Moneda(resultado.IvaLuzValor);
        CostoTotalTexto = MoneyFormat.Moneda(resultado.CostoTotal);
        PrecioVentaTexto = MoneyFormat.Moneda(resultado.PrecioVenta);
        CostoEnvioResultadoTexto = MoneyFormat.Moneda(resultado.CostoEnvio);
        PrecioFinalTexto = MoneyFormat.Moneda(resultado.PrecioFinalConEnvio);
        TiempoCalculadoTexto = "Tiempo de impresión: " + MoneyFormat.Duracion(resultado.HorasImpresion);

        // La fila de envio solo aparece si hay envio (docs/SPEC-legacy.md 3.1.5).
        MostrarEnvio = resultado.CostoEnvio > 0;

        _ultimoResultado = resultado;
        _filamentoDelCalculo = entrada.Filamento;
        _gramosDelCalculo = entrada.Gramos;
        PuedeExportar = true;

        CalculoExitoso?.Invoke(this, EventArgs.Empty);
    }

    private void LimpiarResultados()
    {
        MaterialTexto = ValorCero;
        LuzTexto = ValorCero;
        DesgasteTexto = ValorCero;
        MargenErrorValorTexto = ValorCero;
        IvaLuzValorTexto = ValorCero;
        CostoTotalTexto = ValorCero;
        PrecioVentaTexto = ValorCero;
        CostoEnvioResultadoTexto = ValorCero;
        PrecioFinalTexto = ValorCero;
        TiempoCalculadoTexto = string.Empty;
        MostrarEnvio = false;

        _ultimoResultado = null;
        _filamentoDelCalculo = null;
        _gramosDelCalculo = 0;
        PuedeExportar = false;
    }

    private void MostrarError(string mensaje)
    {
        MensajeError = mensaje;
        HayError = true;
    }

    private void MostrarAdvertencia(string mensaje)
    {
        MensajeAdvertencia = mensaje;
        HayAdvertencia = true;
    }

    private void GestionarFilamentos()
    {
        var gestor = new FilamentManagerViewModel(_datos, _dialogos, _ventanas, Persistir);
        _ventanas.MostrarGestorDeFilamentos(gestor);
        RefrescarFilamentos(FilamentoSeleccionadoId);
    }

    /// <summary>
    /// Repuebla el combo conservando la seleccion por id (B6); si el filamento
    /// elegido ya no existe se pasa al primero de la lista.
    /// </summary>
    private void RefrescarFilamentos(string? idPreferido)
    {
        Filamentos.Clear();
        foreach (var filamento in _datos.Filaments)
        {
            Filamentos.Add(filamento);
        }

        var elegido = _datos.BuscarFilamento(idPreferido) ?? _datos.Filaments.FirstOrDefault();
        FilamentoSeleccionadoId = elegido?.Id;

        OnPropertyChanged(nameof(SinFilamentos));
    }

    private void ExportarPdf()
    {
        if (_ultimoResultado is null || _filamentoDelCalculo is null)
        {
            MostrarError("Primero calculá una cotización y después exportala a PDF.");
            return;
        }

        var datosCliente = new ExportPdfViewModel();
        if (!_ventanas.MostrarDialogoExportacion(datosCliente))
        {
            return;
        }

        var sugerido = "Cotizacion-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".pdf";
        var ruta = _archivos.PedirRutaPdf(sugerido);
        if (string.IsNullOrWhiteSpace(ruta))
        {
            return;
        }

        try
        {
            _pdf.Exportar(
                _ultimoResultado,
                new DatosCotizacionCliente(
                    _datos.Settings.NombreNegocio,
                    datosCliente.Cliente,
                    datosCliente.Trabajo,
                    _filamentoDelCalculo,
                    _gramosDelCalculo,
                    _ultimoResultado.HorasImpresion,
                    DateTime.Now),
                ruta);

            _dialogos.MostrarInformacion("Exportar PDF", $"La cotización se guardó en:{Environment.NewLine}{ruta}");
        }
        catch (Exception ex)
        {
            _dialogos.MostrarError("Exportar PDF", $"No se pudo generar el PDF.{Environment.NewLine}{Environment.NewLine}{ex.Message}");
        }
    }
}
