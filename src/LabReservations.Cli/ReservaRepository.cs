namespace LabReservations.Cli;

using System.Text.Json;
using System.Text.Json.Serialization;

public class ReservaRepository
{
    private readonly string _rutaArchivo;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ReservaRepository(string rutaArchivo = "data/reservas.json")
    {
        _rutaArchivo = rutaArchivo;
    }

    private void AsegurarDirectorio()
    {
        var dir = Path.GetDirectoryName(_rutaArchivo);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public List<Reserva> CargarTodas()
    {
        if (!File.Exists(_rutaArchivo))
        {
            return [];
        }

        var json = File.ReadAllText(_rutaArchivo);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<Reserva>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void GuardarTodas(IEnumerable<Reserva> reservas)
    {
        AsegurarDirectorio();
        var json = JsonSerializer.Serialize(reservas, JsonOptions);

        var tempFile = $"{_rutaArchivo}.tmp";
        File.WriteAllText(tempFile, json);
        File.Move(tempFile, _rutaArchivo, overwrite: true);
    }
}
