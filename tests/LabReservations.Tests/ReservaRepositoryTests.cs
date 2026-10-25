namespace LabReservations.Tests;

using LabReservations.Cli;

public class ReservaRepositoryTests
{
    [Fact]
    public void CargarTodas_CuandoArchivoNoExiste_RetornaListaVacia()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"reservas_inexistente_{Guid.NewGuid():N}.json");
        var repo = new ReservaRepository(tempPath);

        var reservas = repo.CargarTodas();

        Assert.Empty(reservas);
    }

    [Fact]
    public void GuardarTodas_Y_CargarTodas_PersisteYRecuperaReservasIntactas()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"lab_test_{Guid.NewGuid():N}");
        var tempFile = Path.Combine(tempDir, "reservas.json");

        try
        {
            var repoEscritura = new ReservaRepository(tempFile);

            var reservaActiva = new Reserva(
                Id: Guid.NewGuid().ToString("N"),
                UnidadId: "MIC-01",
                Solicitante: "Dr. Ficticio",
                Inicio: new DateTime(2026, 11, 1, 10, 0, 0),
                Fin: new DateTime(2026, 11, 1, 12, 0, 0),
                Estado: EstadoReserva.Activa
            );

            var reservaCancelada = new Reserva(
                Id: Guid.NewGuid().ToString("N"),
                UnidadId: "OSC-01",
                Solicitante: "Investigadora Pérez",
                Inicio: new DateTime(2026, 11, 2, 14, 0, 0),
                Fin: new DateTime(2026, 11, 2, 16, 0, 0),
                Estado: EstadoReserva.Cancelada
            );

            var listaOriginal = new List<Reserva> { reservaActiva, reservaCancelada };

            repoEscritura.GuardarTodas(listaOriginal);

            // Releer desde una nueva instancia para comprobar persistencia real en disco
            var repoLectura = new ReservaRepository(tempFile);
            var reservasLeidas = repoLectura.CargarTodas();

            Assert.Equal(2, reservasLeidas.Count);

            var leidaActiva = reservasLeidas.First(r => r.Id == reservaActiva.Id);
            Assert.Equal("MIC-01", leidaActiva.UnidadId);
            Assert.Equal("Dr. Ficticio", leidaActiva.Solicitante);
            Assert.Equal(new DateTime(2026, 11, 1, 10, 0, 0), leidaActiva.Inicio);
            Assert.Equal(new DateTime(2026, 11, 1, 12, 0, 0), leidaActiva.Fin);
            Assert.Equal(EstadoReserva.Activa, leidaActiva.Estado);

            var leidaCancelada = reservasLeidas.First(r => r.Id == reservaCancelada.Id);
            Assert.Equal("OSC-01", leidaCancelada.UnidadId);
            Assert.Equal("Investigadora Pérez", leidaCancelada.Solicitante);
            Assert.Equal(new DateTime(2026, 11, 2, 14, 0, 0), leidaCancelada.Inicio);
            Assert.Equal(new DateTime(2026, 11, 2, 16, 0, 0), leidaCancelada.Fin);
            Assert.Equal(EstadoReserva.Cancelada, leidaCancelada.Estado);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
