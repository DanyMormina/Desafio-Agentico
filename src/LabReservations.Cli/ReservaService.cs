namespace LabReservations.Cli;

public class ReservaService
{
    private static readonly TimeSpan DuracionMaxima = TimeSpan.FromHours(24);

    private readonly ReservaRepository _repository;
    private readonly Inventario _inventario;

    public ReservaService(ReservaRepository? repository = null, Inventario? inventario = null)
    {
        _repository = repository ?? new ReservaRepository();
        _inventario = inventario ?? Inventario.Default;
    }

    public bool ConsultarDisponibilidad(string unidadId, DateTime inicio, DateTime fin)
    {
        var unidad = ValidarYObtenerUnidad(unidadId);
        ValidarIntervalo(inicio, fin);

        var reservas = _repository.CargarTodas();
        return !ExisteSuperposicionActiva(reservas, unidad.Id, inicio, fin);
    }

    public Reserva CrearReserva(string unidadId, string solicitante, DateTime inicio, DateTime fin)
    {
        var unidad = ValidarYObtenerUnidad(unidadId);

        if (string.IsNullOrWhiteSpace(solicitante))
        {
            throw new ArgumentException("El solicitante no puede estar vacío ni compuesto solo por espacios.", nameof(solicitante));
        }

        ValidarIntervalo(inicio, fin);

        var reservas = _repository.CargarTodas();
        if (ExisteSuperposicionActiva(reservas, unidad.Id, inicio, fin))
        {
            throw new InvalidOperationException($"La unidad '{unidad.Id}' ya posee una reserva activa que se superpone con el horario solicitado.");
        }

        var nuevaReserva = new Reserva(
            Id: GenerarIdReserva(reservas),
            UnidadId: unidad.Id,
            Solicitante: solicitante.Trim(),
            Inicio: inicio,
            Fin: fin,
            Estado: EstadoReserva.Activa
        );

        reservas.Add(nuevaReserva);
        _repository.GuardarTodas(reservas);

        return nuevaReserva;
    }

    public Reserva CancelarReserva(string reservaId)
    {
        if (string.IsNullOrWhiteSpace(reservaId))
        {
            throw new ArgumentException("El identificador de la reserva no puede estar vacío.", nameof(reservaId));
        }

        var idLimpio = reservaId.Trim();
        var reservas = _repository.CargarTodas();
        var indice = reservas.FindIndex(r => string.Equals(r.Id, idLimpio, StringComparison.OrdinalIgnoreCase));

        if (indice < 0)
        {
            throw new KeyNotFoundException($"No se encontró ninguna reserva con el identificador '{idLimpio}'.");
        }

        var reservaCancelada = reservas[indice] with { Estado = EstadoReserva.Cancelada };
        reservas[indice] = reservaCancelada;
        _repository.GuardarTodas(reservas);

        return reservaCancelada;
    }

    public List<Reserva> ObtenerHistorial(string? unidadId = null)
    {
        var reservas = _repository.CargarTodas();

        if (string.IsNullOrWhiteSpace(unidadId))
        {
            return reservas;
        }

        var idLimpio = unidadId.Trim();
        return reservas
            .Where(r => string.Equals(r.UnidadId, idLimpio, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private Unidad ValidarYObtenerUnidad(string unidadId)
    {
        var unidad = _inventario.ObtenerUnidadPorId(unidadId);
        if (unidad is null)
        {
            throw new ArgumentException($"La unidad '{unidadId}' no existe en el inventario del laboratorio.", nameof(unidadId));
        }

        return unidad;
    }

    private static void ValidarIntervalo(DateTime inicio, DateTime fin)
    {
        if (inicio >= fin)
        {
            throw new ArgumentException("La fecha/hora de inicio debe ser estrictamente anterior a la de fin.");
        }

        if (fin - inicio > DuracionMaxima)
        {
            throw new ArgumentException("La duración de la reserva no puede superar las 24 horas continuas.");
        }
    }

    private static bool ExisteSuperposicionActiva(IEnumerable<Reserva> reservas, string unidadId, DateTime inicio, DateTime fin)
    {
        return reservas.Any(existente =>
            existente.Estado == EstadoReserva.Activa &&
            string.Equals(existente.UnidadId, unidadId, StringComparison.OrdinalIgnoreCase) &&
            inicio < existente.Fin &&
            fin > existente.Inicio);
    }

    private static string GenerarIdReserva(IEnumerable<Reserva> existentes)
    {
        var idsExistentes = existentes.Select(r => r.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        string nuevoId;
        do
        {
            nuevoId = $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        } while (idsExistentes.Contains(nuevoId));

        return nuevoId;
    }
}
