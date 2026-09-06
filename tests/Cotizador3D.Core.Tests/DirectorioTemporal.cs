namespace Cotizador3D.Core.Tests;

/// <summary>Directorio temporal aislado que se borra al terminar el test.</summary>
internal sealed class DirectorioTemporal : IDisposable
{
    public DirectorioTemporal()
    {
        Ruta = Path.Combine(Path.GetTempPath(), "cotizador3d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Ruta);
    }

    public string Ruta { get; }

    /// <summary>Ruta de un archivo dentro del directorio temporal.</summary>
    public string Archivo(string nombre = "config_impresion3d.json") => Path.Combine(Ruta, nombre);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Ruta))
            {
                Directory.Delete(Ruta, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }
}
