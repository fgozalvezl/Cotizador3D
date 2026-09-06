namespace Cotizador3D.Core;

/// <summary>
/// Constantes compartidas del nucleo, tomadas de la app legada
/// (ver docs/SPEC-legacy.md seccion 5).
/// </summary>
public static class CoreConstants
{
    /// <summary>Nombre del directorio de la app dentro de LOCALAPPDATA (o del home).</summary>
    public const string NombreDirectorioApp = "Cotizador3D";

    /// <summary>Nombre del archivo de configuracion, identico al legado.</summary>
    public const string NombreArchivoConfig = "config_impresion3d.json";

    /// <summary>Geometria por defecto de la ventana principal ("ANCHOxALTO").</summary>
    public const string GeometryPorDefecto = "950x700";

    /// <summary>Tipos de filamento sugeridos, en el orden exacto del legado.</summary>
    public static IReadOnlyList<string> TiposDeFilamento { get; } = new[]
    {
        "PLA", "PETG", "TPU", "ABS", "ASA", "PLA-CF", "PETG-CF", "Nylon"
    };
}
