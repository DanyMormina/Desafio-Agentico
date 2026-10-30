Console.OutputEncoding = System.Text.Encoding.UTF8;
return Program.Main(args);

#pragma warning disable CS7022
public partial class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        return CliRunner.Run(args, new LabReservations.Cli.ReservaService());
    }
}
#pragma warning restore CS7022

public static class CliRunner
{
    private const string FormatoFechaIso = "yyyy-MM-ddTHH:mm";

    public static int Run(string[] args, LabReservations.Cli.ReservaService service)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("[ERROR] No se especificó ningún comando. Use '--help' para consultar la guía de uso.");
            return 1;
        }

        if (EsAyuda(args[0]))
        {
            ImprimirAyuda();
            return 0;
        }

        var comando = args[0].Trim().ToLowerInvariant();
        var opciones = ParsearOpciones(args.Skip(1).ToArray());

        try
        {
            return comando switch
            {
                "inventario" => EjecutarInventario(),
                "disponibilidad" => EjecutarDisponibilidad(opciones, service),
                "reservar" => EjecutarReservar(opciones, service),
                "cancelar" => EjecutarCancelar(opciones, service),
                "historial" => EjecutarHistorial(opciones, service),
                _ => ErrorArgumento($"Comando desconocido: '{args[0]}'. Use '--help' para ver los comandos disponibles.")
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR INESPERADO] {ex.Message}");
            return 1;
        }
    }

    private static bool EsAyuda(string arg) =>
        arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
        arg.Equals("help", StringComparison.OrdinalIgnoreCase);

    private static int EjecutarInventario()
    {
        Console.WriteLine("INVENTARIO FIJO DE LABORATORIO");
        Console.WriteLine(new string('-', 45));
        Console.WriteLine($"{"ID",-10} | {"EQUIPO",-30}");
        Console.WriteLine(new string('-', 45));

        foreach (var unidad in LabReservations.Cli.Inventario.Todas)
        {
            Console.WriteLine($"{unidad.Id,-10} | {unidad.Nombre,-30}");
        }

        return 0;
    }

    private static int EjecutarDisponibilidad(Dictionary<string, string> opciones, LabReservations.Cli.ReservaService service)
    {
        var unit = ObtenerOpcionRequerida(opciones, "--unit");
        var startStr = ObtenerOpcionRequerida(opciones, "--start");
        var endStr = ObtenerOpcionRequerida(opciones, "--end");

        var inicio = ParsearFechaIso(startStr, "--start");
        var fin = ParsearFechaIso(endStr, "--end");

        var disponible = service.ConsultarDisponibilidad(unit, inicio, fin);
        var estadoTexto = disponible ? "DISPONIBLE" : "OCUPADA";

        Console.WriteLine($"[DISPONIBILIDAD] Unidad: {unit.Trim().ToUpperInvariant()} | Intervalo: {inicio.ToString(FormatoFechaIso)} a {fin.ToString(FormatoFechaIso)} | Estado: {estadoTexto}");
        return 0;
    }

    private static int EjecutarReservar(Dictionary<string, string> opciones, LabReservations.Cli.ReservaService service)
    {
        var unit = ObtenerOpcionRequerida(opciones, "--unit");
        var user = ObtenerOpcionRequerida(opciones, "--user");
        var startStr = ObtenerOpcionRequerida(opciones, "--start");
        var endStr = ObtenerOpcionRequerida(opciones, "--end");

        var inicio = ParsearFechaIso(startStr, "--start");
        var fin = ParsearFechaIso(endStr, "--end");

        var reserva = service.CrearReserva(unit, user, inicio, fin);

        Console.WriteLine("[RESERVA CREADA]");
        Console.WriteLine($"ID: {reserva.Id}");
        Console.WriteLine($"Unidad: {reserva.UnidadId}");
        Console.WriteLine($"Solicitante: {reserva.Solicitante}");
        Console.WriteLine($"Inicio: {reserva.Inicio.ToString(FormatoFechaIso)}");
        Console.WriteLine($"Fin: {reserva.Fin.ToString(FormatoFechaIso)}");
        Console.WriteLine($"Estado: {reserva.Estado.ToString().ToUpperInvariant()}");
        return 0;
    }

    private static int EjecutarCancelar(Dictionary<string, string> opciones, LabReservations.Cli.ReservaService service)
    {
        var id = ObtenerOpcionRequerida(opciones, "--id");
        var reserva = service.CancelarReserva(id);

        Console.WriteLine($"[RESERVA CANCELADA] ID: {reserva.Id} | Unidad: {reserva.UnidadId} | Solicitante: {reserva.Solicitante} | Intervalo: {reserva.Inicio.ToString(FormatoFechaIso)} a {reserva.Fin.ToString(FormatoFechaIso)} | Estado: {reserva.Estado.ToString().ToUpperInvariant()} (Turno liberado, registro conservado)");
        return 0;
    }

    private static int EjecutarHistorial(Dictionary<string, string> opciones, LabReservations.Cli.ReservaService service)
    {
        opciones.TryGetValue("--unit", out var unit);

        if (!string.IsNullOrWhiteSpace(unit) && !LabReservations.Cli.Inventario.Existe(unit))
        {
            throw new ArgumentException($"La unidad '{unit}' no existe en el inventario del laboratorio.");
        }

        var historial = service.ObtenerHistorial(unit);

        if (historial.Count == 0)
        {
            Console.WriteLine("No existen reservas registradas para los criterios consultados.");
            return 0;
        }

        Console.WriteLine($"{"ID",-14} | {"UNIDAD",-8} | {"SOLICITANTE",-20} | {"INICIO",-16} | {"FIN",-16} | {"ESTADO",-10}");
        Console.WriteLine(new string('-', 99));

        foreach (var r in historial)
        {
            Console.WriteLine($"{r.Id,-14} | {r.UnidadId,-8} | {r.Solicitante,-20} | {r.Inicio.ToString(FormatoFechaIso),-16} | {r.Fin.ToString(FormatoFechaIso),-16} | {r.Estado.ToString().ToUpperInvariant(),-10}");
        }

        return 0;
    }

    private static Dictionary<string, string> ParsearOpciones(string[] args)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var actual = args[i];
            if (actual.StartsWith("--"))
            {
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    dict[actual] = args[i + 1];
                    i++;
                }
                else
                {
                    dict[actual] = string.Empty;
                }
            }
        }

        return dict;
    }

    private static string ObtenerOpcionRequerida(Dictionary<string, string> opciones, string clave)
    {
        if (!opciones.TryGetValue(clave, out var valor) || string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException($"Falta el parámetro obligatorio '{clave}'.");
        }

        return valor;
    }

    private static DateTime ParsearFechaIso(string valor, string parametro)
    {
        if (!DateTime.TryParseExact(valor.Trim(), FormatoFechaIso, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fecha))
        {
            throw new ArgumentException($"El parámetro '{parametro}' ('{valor}') no respeta el formato ISO 8601 requerido ({FormatoFechaIso}).");
        }

        return fecha;
    }

    private static int ErrorArgumento(string mensaje)
    {
        Console.Error.WriteLine($"[ERROR] {mensaje}");
        return 1;
    }

    private static void ImprimirAyuda()
    {
        Console.WriteLine("""
            LabReservations CLI (.NET 10) - Gestión de Reservas de Laboratorio
            Uso: dotnet run --project src/LabReservations.Cli -- <comando> [opciones]

            Comandos disponibles:
              inventario
                  Lista las 5 unidades físicas del inventario fijo precargado.

              disponibilidad --unit <ID> --start <yyyy-MM-ddTHH:mm> --end <yyyy-MM-ddTHH:mm>
                  Consulta si una unidad está DISPONIBLE u OCUPADA en el intervalo indicado.

              reservar --unit <ID> --user "<Nombre>" --start <yyyy-MM-ddTHH:mm> --end <yyyy-MM-ddTHH:mm>
                  Registra una nueva reserva activa validando inventario, horario y superposiciones.

              cancelar --id <ID_RESERVA>
                  Marca una reserva como CANCELADA liberando el horario sin borrar el registro histórico.

              historial [--unit <ID>]
                  Lista todas las reservas registradas (activas y canceladas), filtrando opcionalmente por unidad.

              --help | -h
                  Muestra esta guía de uso.

            Ejemplos:
              dotnet run --project src/LabReservations.Cli -- inventario
              dotnet run --project src/LabReservations.Cli -- disponibilidad --unit MIC-01 --start 2026-10-27T10:00 --end 2026-10-27T11:00
              dotnet run --project src/LabReservations.Cli -- reservar --unit MIC-01 --user "Ana Perez" --start 2026-10-27T10:00 --end 2026-10-27T11:00
              dotnet run --project src/LabReservations.Cli -- cancelar --id RES-1A2B3C4D
              dotnet run --project src/LabReservations.Cli -- historial --unit MIC-01
            """);
    }
}
