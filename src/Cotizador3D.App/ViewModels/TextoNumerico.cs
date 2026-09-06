using Cotizador3D.Core.Formatting;

namespace Cotizador3D.App.ViewModels;

/// <summary>
/// Convierte numeros a texto editable para los campos de entrada: coma
/// decimal (es-AR, B9), sin separador de miles y sin ceros sobrantes, de forma
/// que <c>NumberParser</c> pueda volver a leerlos tal cual.
/// </summary>
internal static class TextoNumerico
{
    public static string Formatear(double valor) =>
        valor.ToString("0.##########", MoneyFormat.CulturaEsAr);
}
