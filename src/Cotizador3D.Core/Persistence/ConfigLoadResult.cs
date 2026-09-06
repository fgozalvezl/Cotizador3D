using Cotizador3D.Core.Models;

namespace Cotizador3D.Core.Persistence;

/// <summary>
/// Resultado de <see cref="ConfigStore.Cargar"/>. Distingue los dos casos que
/// antes se confundian: "no hay archivo todavia" (defaults, se puede guardar) y
/// "el archivo existe pero no se pudo leer" (defaults, pero guardar
/// sobrescribiria la configuracion real del usuario).
/// </summary>
public sealed class ConfigLoadResult
{
    private ConfigLoadResult(AppData datos, bool cargaFallida, string? motivo)
    {
        Datos = datos;
        CargaFallida = cargaFallida;
        Motivo = motivo;
    }

    /// <summary>Datos cargados; si la carga fallo, son los valores por defecto.</summary>
    public AppData Datos { get; }

    /// <summary>
    /// true si habia un archivo pero no se pudo leer o interpretar. En ese caso
    /// la app NO debe guardar: pisaria la configuracion real del usuario.
    /// </summary>
    public bool CargaFallida { get; }

    /// <summary>Motivo del fallo, en espanol, para mostrarselo al usuario.</summary>
    public string? Motivo { get; }

    /// <summary>Carga correcta (incluye "el archivo todavia no existe").</summary>
    public static ConfigLoadResult Exito(AppData datos)
    {
        ArgumentNullException.ThrowIfNull(datos);
        return new ConfigLoadResult(datos, cargaFallida: false, motivo: null);
    }

    /// <summary>El archivo existe pero no se pudo leer o interpretar.</summary>
    public static ConfigLoadResult Fallo(string motivo) =>
        new(AppData.PorDefecto(), cargaFallida: true, motivo);
}
