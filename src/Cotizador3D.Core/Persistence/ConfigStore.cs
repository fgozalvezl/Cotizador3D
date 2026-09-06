using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Models;

namespace Cotizador3D.Core.Persistence;

/// <summary>
/// Lee y escribe <c>config_impresion3d.json</c> con la MISMA ruta y el MISMO
/// esquema que la app Python (docs/SPEC-legacy.md 1), para que el usuario no
/// pierda su configuracion al migrar.
/// <para>
/// Carga tolerante (B1/B2): archivo inexistente, vacio, corrupto o con claves
/// faltantes devuelve los valores por defecto. Los numeros se aceptan como
/// string o como numero JSON (B13). Guardado atomico (B11/B21) y el directorio
/// se crea solo al guardar (B19).
/// </para>
/// </summary>
public sealed class ConfigStore
{
    private const string ClaveSettings = "settings";
    private const string ClaveFilaments = "filaments";

    private static readonly JsonWriterOptions OpcionesEscritura = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Usa la ruta por defecto del sistema (la misma que el legado).</summary>
    public ConfigStore()
        : this(ObtenerRutaPorDefecto())
    {
    }

    /// <summary>Usa una ruta explicita (para tests o instalaciones portables).</summary>
    public ConfigStore(string rutaArchivo)
    {
        if (string.IsNullOrWhiteSpace(rutaArchivo))
        {
            throw new ArgumentException("La ruta del archivo de configuración no puede estar vacía.", nameof(rutaArchivo));
        }

        RutaArchivo = Path.GetFullPath(rutaArchivo);
    }

    /// <summary>Ruta absoluta del archivo de configuracion que usa esta instancia.</summary>
    public string RutaArchivo { get; }

    /// <summary>Directorio que contiene el archivo de configuracion.</summary>
    public string DirectorioArchivo => Path.GetDirectoryName(RutaArchivo) ?? string.Empty;

    /// <summary>
    /// Ruta legada: <c>%LOCALAPPDATA%/Cotizador3D/config_impresion3d.json</c>,
    /// con fallback al home del usuario si LOCALAPPDATA no existe o esta vacia.
    /// No crea ningun directorio (B19).
    /// </summary>
    public static string ObtenerRutaPorDefecto()
    {
        var baseDir = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Directory.GetCurrentDirectory();
        }

        return Path.Combine(baseDir, CoreConstants.NombreDirectorioApp, CoreConstants.NombreArchivoConfig);
    }

    /// <summary>
    /// Carga los datos. Nunca lanza por contenido invalido: cualquier problema
    /// de formato cae a los valores por defecto.
    /// </summary>
    public AppData Cargar()
    {
        string contenido;
        try
        {
            if (!File.Exists(RutaArchivo))
            {
                return AppData.PorDefecto();
            }

            contenido = File.ReadAllText(RutaArchivo, Encoding.UTF8);
        }
        catch (IOException)
        {
            return AppData.PorDefecto();
        }
        catch (UnauthorizedAccessException)
        {
            return AppData.PorDefecto();
        }

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return AppData.PorDefecto();
        }

        try
        {
            using var documento = JsonDocument.Parse(contenido, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            var raiz = documento.RootElement;
            if (raiz.ValueKind != JsonValueKind.Object)
            {
                return AppData.PorDefecto();
            }

            return new AppData
            {
                Settings = LeerSettings(raiz),
                Filaments = LeerFilamentos(raiz),
            };
        }
        catch (JsonException)
        {
            return AppData.PorDefecto();
        }
    }

    /// <summary>
    /// Guarda los datos con el esquema legado (valores numericos como string,
    /// B13). Escritura atomica: archivo temporal + reemplazo. Crea el
    /// directorio si hace falta.
    /// </summary>
    /// <exception cref="IOException">Si no se puede escribir el archivo.</exception>
    public void Guardar(AppData datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        var directorio = DirectorioArchivo;
        if (!string.IsNullOrEmpty(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        var json = Serializar(datos);

        var temporal = RutaArchivo + ".tmp";
        try
        {
            File.WriteAllText(temporal, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporal, RutaArchivo, overwrite: true);
        }
        catch
        {
            TryBorrar(temporal);
            throw;
        }
    }

    /// <summary>Serializa los datos al texto JSON exacto que se persiste.</summary>
    public static string Serializar(AppData datos)
    {
        ArgumentNullException.ThrowIfNull(datos);

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, OpcionesEscritura))
        {
            var s = datos.Settings;

            writer.WriteStartObject();
            writer.WriteStartObject(ClaveSettings);
            EscribirNumeroComoString(writer, "precio_kwh", s.PrecioKwh);
            EscribirNumeroComoString(writer, "consumo_w", s.ConsumoW);
            EscribirNumeroComoString(writer, "desgaste_horas", s.DesgasteHoras);
            EscribirNumeroComoString(writer, "precio_repuestos", s.PrecioRepuestos);
            EscribirNumeroComoString(writer, "margen_error_pct", s.MargenErrorPct);
            EscribirNumeroComoString(writer, "iva_luz_pct", s.IvaLuzPct);
            EscribirNumeroComoString(writer, "margen_ganancia_x", s.MargenGanancia);
            EscribirNumeroComoString(writer, "costo_envio", s.CostoEnvio);
            writer.WriteString("geometry", s.Geometry ?? CoreConstants.GeometryPorDefecto);
            writer.WriteString("nombre_negocio", s.NombreNegocio ?? string.Empty);
            writer.WriteEndObject();

            writer.WriteStartArray(ClaveFilaments);
            foreach (var filamento in datos.Filaments)
            {
                writer.WriteStartObject();
                writer.WriteString("brand", filamento.Brand ?? string.Empty);
                writer.WriteString("type", filamento.Type ?? string.Empty);
                writer.WriteNumber("price_kg", filamento.PriceKg);
                writer.WriteString("id", filamento.Id ?? string.Empty);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void EscribirNumeroComoString(Utf8JsonWriter writer, string nombre, double valor) =>
        writer.WriteString(nombre, NumberParser.ToInvariantString(valor));

    private static AppSettings LeerSettings(JsonElement raiz)
    {
        var settings = new AppSettings();

        if (!raiz.TryGetProperty(ClaveSettings, out var nodo) || nodo.ValueKind != JsonValueKind.Object)
        {
            return settings;
        }

        settings.PrecioKwh = LeerNumero(nodo, "precio_kwh", AppSettings.PrecioKwhPorDefecto);
        settings.ConsumoW = LeerNumero(nodo, "consumo_w", AppSettings.ConsumoWPorDefecto);
        settings.DesgasteHoras = LeerNumero(nodo, "desgaste_horas", AppSettings.DesgasteHorasPorDefecto);
        settings.PrecioRepuestos = LeerNumero(nodo, "precio_repuestos", AppSettings.PrecioRepuestosPorDefecto);
        settings.MargenErrorPct = LeerNumero(nodo, "margen_error_pct", AppSettings.MargenErrorPctPorDefecto);
        settings.IvaLuzPct = LeerNumero(nodo, "iva_luz_pct", AppSettings.IvaLuzPctPorDefecto);
        settings.MargenGanancia = LeerNumero(nodo, "margen_ganancia_x", AppSettings.MargenGananciaPorDefecto);
        settings.CostoEnvio = LeerNumero(nodo, "costo_envio", AppSettings.CostoEnvioPorDefecto);
        settings.Geometry = LeerTexto(nodo, "geometry", CoreConstants.GeometryPorDefecto);
        settings.NombreNegocio = LeerTexto(nodo, "nombre_negocio", AppSettings.NombreNegocioPorDefecto);

        return settings;
    }

    private static List<Filament> LeerFilamentos(JsonElement raiz)
    {
        var filamentos = new List<Filament>();

        if (!raiz.TryGetProperty(ClaveFilaments, out var nodo) || nodo.ValueKind != JsonValueKind.Array)
        {
            return filamentos;
        }

        var vistos = new HashSet<string>(StringComparer.Ordinal);

        foreach (var elemento in nodo.EnumerateArray())
        {
            if (elemento.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            filamentos.Add(new Filament
            {
                Id = LeerId(elemento, vistos),
                Brand = LeerTexto(elemento, "brand", string.Empty),
                Type = LeerTexto(elemento, "type", string.Empty),
                PriceKg = LeerNumero(elemento, "price_kg", 0d),
            });
        }

        return filamentos;
    }

    /// <summary>
    /// B6: cada filamento necesita un id unico. El id del archivo se conserva
    /// tal cual (los del legado eran "marca_tipo_hex"); si falta, esta vacio o
    /// esta repetido, se genera uno nuevo.
    /// </summary>
    private static string LeerId(JsonElement elemento, HashSet<string> vistos)
    {
        if (elemento.TryGetProperty("id", out var nodo)
            && nodo.ValueKind == JsonValueKind.String)
        {
            var id = nodo.GetString();
            if (!string.IsNullOrWhiteSpace(id) && vistos.Add(id))
            {
                return id;
            }
        }

        string nuevo;
        do
        {
            nuevo = Filament.NuevoId();
        }
        while (!vistos.Add(nuevo));

        return nuevo;
    }

    /// <summary>B13: el valor puede venir como numero JSON o como string.</summary>
    private static double LeerNumero(JsonElement objeto, string clave, double porDefecto)
    {
        if (!objeto.TryGetProperty(clave, out var nodo))
        {
            return porDefecto;
        }

        return nodo.ValueKind switch
        {
            JsonValueKind.Number => nodo.TryGetDouble(out var numero) ? numero : porDefecto,
            JsonValueKind.String => NumberParser.ParseOrDefault(nodo.GetString(), porDefecto),
            _ => porDefecto,
        };
    }

    private static string LeerTexto(JsonElement objeto, string clave, string porDefecto)
    {
        if (!objeto.TryGetProperty(clave, out var nodo))
        {
            return porDefecto;
        }

        return nodo.ValueKind switch
        {
            JsonValueKind.String => nodo.GetString() ?? porDefecto,
            JsonValueKind.Number => nodo.GetRawText(),
            _ => porDefecto,
        };
    }

    private static void TryBorrar(string ruta)
    {
        try
        {
            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
        catch (IOException)
        {
            // El temporal quedo huerfano; no es motivo para ocultar el error real.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
