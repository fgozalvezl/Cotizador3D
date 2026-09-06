using System.Text.Json;

namespace Cotizador3D.Core.Models;

/// <summary>
/// Contenido completo de <c>config_impresion3d.json</c>: el objeto
/// <c>settings</c> y el array <c>filaments</c>.
/// </summary>
public sealed class AppData
{
    public AppSettings Settings { get; set; } = new();

    public List<Filament> Filaments { get; set; } = new();

    /// <summary>
    /// Claves desconocidas del objeto raiz del JSON. Se conservan tal cual y se
    /// vuelven a escribir al guardar, para no destruir datos de otras versiones
    /// de la app.
    /// </summary>
    public Dictionary<string, JsonElement> Extras { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Datos por defecto, equivalentes a <c>get_default_data()</c> del legado.</summary>
    public static AppData PorDefecto() => new();

    /// <summary>Busca un filamento por id; devuelve null si no existe.</summary>
    public Filament? BuscarFilamento(string? id) =>
        string.IsNullOrEmpty(id) ? null : Filaments.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.Ordinal));

    public AppData Clonar() => new()
    {
        Settings = Settings.Clonar(),
        Filaments = Filaments.Select(f => f.Clonar()).ToList(),
        Extras = new Dictionary<string, JsonElement>(Extras, StringComparer.Ordinal),
    };
}
