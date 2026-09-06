using System.Text.Json;

namespace Cotizador3D.Core.Models;

/// <summary>
/// Un filamento de la biblioteca del usuario. Corresponde a un elemento del
/// array "filaments" del JSON legado (docs/SPEC-legacy.md 1.2.2).
/// </summary>
public sealed class Filament
{
    /// <summary>
    /// Identificador estable del filamento (decision B6). Es un string libre:
    /// se conservan tal cual los ids del legado ("marca_tipo_hex"); los
    /// filamentos sin <c>id</c> (o con id repetido) reciben uno nuevo.
    /// </summary>
    public string Id { get; set; } = NuevoId();

    /// <summary>Marca. Clave JSON: <c>brand</c>.</summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>Tipo (PLA, PETG, ...). Clave JSON: <c>type</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Precio por kilogramo. Clave JSON: <c>price_kg</c>.</summary>
    public double PriceKg { get; set; }

    /// <summary>
    /// Claves desconocidas de este filamento en el JSON. Se conservan tal cual
    /// y se vuelven a escribir al guardar.
    /// </summary>
    public Dictionary<string, JsonElement> Extras { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Texto visible en la UI, con el mismo formato que el legado: "marca (tipo)".</summary>
    public string DisplayName => $"{Brand} ({Type})";

    public Filament Clonar() => new()
    {
        Id = Id,
        Brand = Brand,
        Type = Type,
        PriceKg = PriceKg,
        Extras = new Dictionary<string, JsonElement>(Extras, StringComparer.Ordinal),
    };

    public override string ToString() => DisplayName;

    /// <summary>Genera un identificador nuevo (GUID sin guiones).</summary>
    public static string NuevoId() => Guid.NewGuid().ToString("N");
}
