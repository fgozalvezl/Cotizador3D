using System.Globalization;
using System.Text.RegularExpressions;
using Cotizador3D.Core;

namespace Cotizador3D.App.Services;

/// <summary>
/// Geometria de la ventana principal tal como se guarda en
/// <c>settings.geometry</c> (docs/SPEC-legacy.md seccion 4): "ANCHOxALTO" o
/// "ANCHOxALTO+X+Y". Se acepta cualquiera de las dos formas al leer; al
/// escribir se incluye la posicion solo si no es negativa, para que el string
/// siga siendo valido para la app Python legada.
/// </summary>
public sealed class GeometriaVentana
{
    private static readonly Regex Patron = new(
        @"^\s*(\d{1,5})\s*[xX]\s*(\d{1,5})\s*(?:\+\s*(-?\d{1,5})\s*\+\s*(-?\d{1,5})\s*)?$",
        RegexOptions.CultureInvariant);

    public GeometriaVentana(int ancho, int alto, int? izquierda = null, int? arriba = null)
    {
        Ancho = ancho;
        Alto = alto;
        Izquierda = izquierda;
        Arriba = arriba;
    }

    public int Ancho { get; }

    public int Alto { get; }

    public int? Izquierda { get; }

    public int? Arriba { get; }

    public bool TienePosicion => Izquierda.HasValue && Arriba.HasValue;

    /// <summary>Geometria por defecto del legado (950x700, sin posicion).</summary>
    public static GeometriaVentana PorDefecto() => Parsear(CoreConstants.GeometryPorDefecto);

    /// <summary>Parsea el string; si no es valido devuelve 950x700.</summary>
    public static GeometriaVentana Parsear(string? texto)
    {
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var m = Patron.Match(texto);
            if (m.Success
                && int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ancho)
                && int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var alto)
                && ancho > 0
                && alto > 0)
            {
                if (m.Groups[3].Success
                    && int.TryParse(m.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                    && int.TryParse(m.Groups[4].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                {
                    return new GeometriaVentana(ancho, alto, x, y);
                }

                return new GeometriaVentana(ancho, alto);
            }
        }

        // Fallback literal, sin recursion: el default del legado es "950x700".
        return new GeometriaVentana(950, 700);
    }

    /// <summary>Serializa a "ANCHOxALTO" o "ANCHOxALTO+X+Y".</summary>
    public string Formatear()
    {
        var tamano = string.Create(CultureInfo.InvariantCulture, $"{Ancho}x{Alto}");
        if (Izquierda is int x && Arriba is int y && x >= 0 && y >= 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{tamano}+{x}+{y}");
        }

        return tamano;
    }
}
