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
/// Carga tolerante (B1/B2): las claves faltantes toman el valor por defecto y
/// los numeros se aceptan como string o como numero JSON (B13). Pero un
/// archivo que existe y NO se puede leer o interpretar se informa como fallo
/// (<see cref="ConfigLoadResult.CargaFallida"/>) para que la app no lo pise con
/// los defaults. Guardado atomico (B11/B21) con copia de seguridad previa, y el
/// directorio se crea solo al guardar (B19).
/// </para>
/// </summary>
public sealed class ConfigStore
{
    /// <summary>Sufijo de la copia de seguridad que se hace antes de sobrescribir.</summary>
    public const string SufijoCopiaDeSeguridad = ".bak";

    private const string ClaveSettings = "settings";
    private const string ClaveFilaments = "filaments";

    private static readonly JsonWriterOptions OpcionesEscritura = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly HashSet<string> ClavesRaizConocidas =
        new(StringComparer.Ordinal) { ClaveSettings, ClaveFilaments };

    private static readonly HashSet<string> ClavesSettingsConocidas = new(StringComparer.Ordinal)
    {
        "precio_kwh", "consumo_w", "desgaste_horas", "precio_repuestos",
        "margen_error_pct", "iva_luz_pct", "margen_ganancia_x", "costo_envio",
        "geometry", "nombre_negocio",
    };

    private static readonly HashSet<string> ClavesFilamentoConocidas =
        new(StringComparer.Ordinal) { "brand", "type", "price_kg", "id" };

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

    /// <summary>Ruta de la copia de seguridad: la del archivo mas <c>.bak</c>.</summary>
    public string RutaCopiaDeSeguridad => RutaArchivo + SufijoCopiaDeSeguridad;

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
    /// Carga los datos. Nunca lanza: devuelve siempre un
    /// <see cref="ConfigLoadResult"/>. Si no hay archivo, son los valores por
    /// defecto y la carga se considera correcta; si el archivo existe pero no
    /// se puede leer o su contenido no es un objeto JSON valido, se devuelven
    /// los defaults con <see cref="ConfigLoadResult.CargaFallida"/> en true.
    /// </summary>
    public ConfigLoadResult Cargar()
    {
        if (!File.Exists(RutaArchivo) && !Directory.Exists(RutaArchivo))
        {
            return ConfigLoadResult.Exito(AppData.PorDefecto());
        }

        string contenido;
        try
        {
            contenido = File.ReadAllText(RutaArchivo, Encoding.UTF8);
        }
        catch (FileNotFoundException)
        {
            // Desaparecio entre el File.Exists y la lectura: es "no hay archivo".
            return ConfigLoadResult.Exito(AppData.PorDefecto());
        }
        catch (DirectoryNotFoundException)
        {
            return ConfigLoadResult.Exito(AppData.PorDefecto());
        }
        catch (IOException ex)
        {
            return ConfigLoadResult.Fallo($"No se pudo leer el archivo. Detalle: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ConfigLoadResult.Fallo($"No se pudo leer el archivo. Detalle: {ex.Message}");
        }

        // Un archivo vacio no tiene nada que perder: se trata como el legado
        // (docs/SPEC-legacy.md 1.3, punto 3) y se permite guardar.
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return ConfigLoadResult.Exito(AppData.PorDefecto());
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
                return ConfigLoadResult.Fallo(
                    "El contenido del archivo no es un objeto JSON con la configuración.");
            }

            return ConfigLoadResult.Exito(new AppData
            {
                Settings = LeerSettings(raiz),
                Filaments = LeerFilamentos(raiz),
                Extras = LeerExtras(raiz, ClavesRaizConocidas),
            });
        }
        catch (JsonException ex)
        {
            return ConfigLoadResult.Fallo($"El archivo está dañado y no se pudo interpretar. Detalle: {ex.Message}");
        }
    }

    /// <summary>
    /// Guarda los datos con el esquema legado (valores numericos como string,
    /// B13). Antes de reemplazar un archivo existente hace una copia en
    /// <see cref="RutaCopiaDeSeguridad"/> (best-effort). Escritura atomica:
    /// archivo temporal + reemplazo. Crea el directorio si hace falta.
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

        TryCopiaDeSeguridad();

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
            EscribirExtras(writer, s.Extras, ClavesSettingsConocidas);
            writer.WriteEndObject();

            writer.WriteStartArray(ClaveFilaments);
            foreach (var filamento in datos.Filaments)
            {
                writer.WriteStartObject();
                writer.WriteString("brand", filamento.Brand ?? string.Empty);
                writer.WriteString("type", filamento.Type ?? string.Empty);
                writer.WriteNumber("price_kg", filamento.PriceKg);
                writer.WriteString("id", filamento.Id ?? string.Empty);
                EscribirExtras(writer, filamento.Extras, ClavesFilamentoConocidas);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            EscribirExtras(writer, datos.Extras, ClavesRaizConocidas);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void EscribirNumeroComoString(Utf8JsonWriter writer, string nombre, double valor) =>
        writer.WriteString(nombre, NumberParser.ToInvariantString(valor));

    /// <summary>
    /// Vuelve a escribir las claves desconocidas que traia el archivo, para no
    /// perder datos de otras versiones de la app. Las claves que este
    /// serializador ya escribe se descartan (nunca se duplica una clave).
    /// </summary>
    private static void EscribirExtras(
        Utf8JsonWriter writer,
        Dictionary<string, JsonElement>? extras,
        HashSet<string> conocidas)
    {
        if (extras is null)
        {
            return;
        }

        foreach (var (clave, valor) in extras)
        {
            if (string.IsNullOrEmpty(clave) || conocidas.Contains(clave) || valor.ValueKind == JsonValueKind.Undefined)
            {
                continue;
            }

            writer.WritePropertyName(clave);
            valor.WriteTo(writer);
        }
    }

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
        settings.Extras = LeerExtras(nodo, ClavesSettingsConocidas);

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
                Extras = LeerExtras(elemento, ClavesFilamentoConocidas),
            });
        }

        return filamentos;
    }

    /// <summary>
    /// Claves del objeto que este lector no conoce. Se clonan porque el
    /// <see cref="JsonDocument"/> se libera al terminar la carga.
    /// </summary>
    private static Dictionary<string, JsonElement> LeerExtras(JsonElement objeto, HashSet<string> conocidas)
    {
        var extras = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var propiedad in objeto.EnumerateObject())
        {
            if (conocidas.Contains(propiedad.Name))
            {
                continue;
            }

            extras[propiedad.Name] = propiedad.Value.Clone();
        }

        return extras;
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

    /// <summary>
    /// Copia el archivo actual a <c>.bak</c> antes de reemplazarlo. Es
    /// best-effort: si falla, el guardado sigue igual (la copia es una red de
    /// seguridad, no un requisito para guardar).
    /// </summary>
    private void TryCopiaDeSeguridad()
    {
        try
        {
            if (File.Exists(RutaArchivo))
            {
                File.Copy(RutaArchivo, RutaCopiaDeSeguridad, overwrite: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
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
