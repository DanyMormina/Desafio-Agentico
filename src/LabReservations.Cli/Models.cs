namespace LabReservations.Cli;

public enum EstadoReserva
{
    Activa,
    Cancelada
}

public record Unidad(string Id, string Nombre);

public record Reserva(
    string Id,
    string UnidadId,
    string Solicitante,
    DateTime Inicio,
    DateTime Fin,
    EstadoReserva Estado
);
