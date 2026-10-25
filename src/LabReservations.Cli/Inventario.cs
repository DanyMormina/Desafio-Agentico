namespace LabReservations.Cli;

using System.Text.Json;

public class Inventario
{
    private static readonly List<Unidad> UnidadesPorDefecto =
    [
        new("MIC-01", "Microscopio digital"),
        new("MIC-02", "Microscopio digital"),
        new("OSC-01", "Osciloscopio"),
        new("MUL-01", "Multímetro"),
        new("FUE-01", "Fuente de alimentación")
    ];

    private static readonly Lazy<Inventario> InstanciaCompartida = new(() => new Inventario());

    /// <summary>
    /// Instancia por defecto que intenta leer data/inventario.json o usa el fallback en memoria.
    /// </summary>
    public static Inventario Default => InstanciaCompartida.Value;

    public IReadOnlyList<Unidad> Unidades { get; }

    public Inventario(string rutaArchivo = "data/inventario.json")
    {
        Unidades = CargarDesdeArchivo(rutaArchivo);
    }

    private static IReadOnlyList<Unidad> CargarDesdeArchivo(string rutaArchivo)
    {
        try
        {
            if (File.Exists(rutaArchivo))
            {
                var json = File.ReadAllText(rutaArchivo);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var opciones = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var unidades = JsonSerializer.Deserialize<List<Unidad>>(json, opciones);
                    if (unidades is { Count: > 0 })
                    {
                        return unidades.AsReadOnly();
                    }
                }
            }
        }
        catch
        {
            // Fallback silencioso y seguro a memoria ante problemas de I/O o formato
        }

        return UnidadesPorDefecto.AsReadOnly();
    }

    public bool ExisteUnidad(string? unidadId)
    {
        if (string.IsNullOrWhiteSpace(unidadId))
        {
            return false;
        }

        var idLimpio = unidadId.Trim();
        return Unidades.Any(u => string.Equals(u.Id, idLimpio, StringComparison.OrdinalIgnoreCase));
    }

    public Unidad? ObtenerUnidadPorId(string? unidadId)
    {
        if (string.IsNullOrWhiteSpace(unidadId))
        {
            return null;
        }

        var idLimpio = unidadId.Trim();
        return Unidades.FirstOrDefault(u => string.Equals(u.Id, idLimpio, StringComparison.OrdinalIgnoreCase));
    }

    // Helpers estáticos de acceso directo para tests y CLI
    public static bool Existe(string? unidadId) => Default.ExisteUnidad(unidadId);
    public static Unidad? ObtenerPorId(string? unidadId) => Default.ObtenerUnidadPorId(unidadId);
    public static IReadOnlyList<Unidad> Todas => Default.Unidades;
}
