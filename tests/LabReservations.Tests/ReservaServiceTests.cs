namespace LabReservations.Tests;

using LabReservations.Cli;

public class ReservaServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;
    private readonly ReservaRepository _repository;
    private readonly ReservaService _service;

    public ReservaServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lab_service_test_{Guid.NewGuid():N}");
        _tempFile = Path.Combine(_tempDir, "reservas.json");
        _repository = new ReservaRepository(_tempFile);
        _service = new ReservaService(_repository, Inventario.Default);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // 1. Creación válida de reserva
    [Fact]
    public void CrearReserva_ConDatosValidos_CreaReservaActivaYPersiste()
    {
        var inicio = new DateTime(2026, 11, 10, 9, 0, 0);
        var fin = new DateTime(2026, 11, 10, 11, 0, 0);

        var reserva = _service.CrearReserva("MIC-01", "  Dra. Valeria Gómez  ", inicio, fin);

        Assert.NotNull(reserva);
        Assert.StartsWith("RES-", reserva.Id);
        Assert.Equal("MIC-01", reserva.UnidadId);
        Assert.Equal("Dra. Valeria Gómez", reserva.Solicitante);
        Assert.Equal(inicio, reserva.Inicio);
        Assert.Equal(fin, reserva.Fin);
        Assert.Equal(EstadoReserva.Activa, reserva.Estado);

        var historial = _service.ObtenerHistorial("MIC-01");
        Assert.Single(historial);
        Assert.Equal(reserva.Id, historial[0].Id);
    }

    // 2. Rechazo de superposición en la misma unidad
    [Theory]
    [InlineData(10, 30, 11, 30)] // Solapamiento parcial al final (10:00-11:00 vs 10:30-11:30)
    [InlineData(9, 30, 10, 30)]  // Solapamiento parcial al inicio (10:00-11:00 vs 09:30-10:30)
    [InlineData(10, 0, 11, 0)]   // Solapamiento exacto (10:00-11:00 vs 10:00-11:00)
    [InlineData(10, 15, 10, 45)] // Contenida totalmente dentro de la existente
    [InlineData(9, 0, 12, 0)]    // Envuelve totalmente a la existente
    public void CrearReserva_ConSuperposicionEnMismaUnidad_LanzaInvalidOperationException(
        int horaInicio, int minInicio, int horaFin, int minFin)
    {
        var baseInicio = new DateTime(2026, 11, 15, 10, 0, 0);
        var baseFin = new DateTime(2026, 11, 15, 11, 0, 0);
        _service.CrearReserva("MIC-01", "Solicitante 1", baseInicio, baseFin);

        var nuevoInicio = new DateTime(2026, 11, 15, horaInicio, minInicio, 0);
        var nuevoFin = new DateTime(2026, 11, 15, horaFin, minFin, 0);

        Assert.False(_service.ConsultarDisponibilidad("MIC-01", nuevoInicio, nuevoFin));
        Assert.Throws<InvalidOperationException>(() =>
            _service.CrearReserva("MIC-01", "Solicitante 2", nuevoInicio, nuevoFin));
    }

    // 3. Aceptación de límites consecutivos exactos en la misma unidad
    [Fact]
    public void CrearReserva_ConLimitesConsecutivosExactosEnMismaUnidad_PermiteAmbasReservas()
    {
        var t1 = new DateTime(2026, 11, 15, 10, 0, 0);
        var t2 = new DateTime(2026, 11, 15, 11, 0, 0);
        var t3 = new DateTime(2026, 11, 15, 12, 0, 0);

        var r1 = _service.CrearReserva("OSC-01", "Equipo A", t1, t2);
        Assert.True(_service.ConsultarDisponibilidad("OSC-01", t2, t3));

        var r2 = _service.CrearReserva("OSC-01", "Equipo B", t2, t3);

        Assert.NotEqual(r1.Id, r2.Id);
        Assert.Equal(2, _service.ObtenerHistorial("OSC-01").Count);
    }

    // 4. Aceptación de reservas simultáneas en unidades diferentes
    [Fact]
    public void CrearReserva_SimultaneaEnUnidadesDiferentes_PermiteAmbasReservas()
    {
        var inicio = new DateTime(2026, 11, 20, 14, 0, 0);
        var fin = new DateTime(2026, 11, 20, 16, 0, 0);

        var rMic01 = _service.CrearReserva("MIC-01", "Investigador A", inicio, fin);
        Assert.True(_service.ConsultarDisponibilidad("MIC-02", inicio, fin));

        var rMic02 = _service.CrearReserva("MIC-02", "Investigador B", inicio, fin);

        Assert.Equal(EstadoReserva.Activa, rMic01.Estado);
        Assert.Equal(EstadoReserva.Activa, rMic02.Estado);
        Assert.Equal(2, _service.ObtenerHistorial().Count);
    }

    // 5. Rechazo de entradas inválidas
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void CrearReserva_ConSolicitanteVacioOEspacios_LanzaArgumentException(string solicitanteInvalido)
    {
        var inicio = new DateTime(2026, 11, 10, 10, 0, 0);
        var fin = new DateTime(2026, 11, 10, 11, 0, 0);

        Assert.Throws<ArgumentException>(() =>
            _service.CrearReserva("MIC-01", solicitanteInvalido, inicio, fin));
    }

    [Theory]
    [InlineData("MIC-99")]
    [InlineData("INEXISTENTE")]
    [InlineData("")]
    [InlineData("   ")]
    public void CrearReserva_Y_ConsultarDisponibilidad_ConUnidadDesconocida_LanzaArgumentException(string unidadInvalida)
    {
        var inicio = new DateTime(2026, 11, 10, 10, 0, 0);
        var fin = new DateTime(2026, 11, 10, 11, 0, 0);

        Assert.Throws<ArgumentException>(() =>
            _service.ConsultarDisponibilidad(unidadInvalida, inicio, fin));

        Assert.Throws<ArgumentException>(() =>
            _service.CrearReserva(unidadInvalida, "Solicitante Válido", inicio, fin));
    }

    [Fact]
    public void CrearReserva_ConInicioMayorOIgualAFin_LanzaArgumentException()
    {
        var fecha = new DateTime(2026, 11, 10, 10, 0, 0);

        // Inicio == Fin
        Assert.Throws<ArgumentException>(() =>
            _service.CrearReserva("MUL-01", "Solicitante", fecha, fecha));

        // Inicio > Fin
        Assert.Throws<ArgumentException>(() =>
            _service.CrearReserva("MUL-01", "Solicitante", fecha.AddHours(2), fecha));
    }

    [Fact]
    public void CrearReserva_ConDuracionMayorA24Horas_LanzaArgumentException()
    {
        var inicio = new DateTime(2026, 11, 10, 10, 0, 0);
        var finExacto24h = inicio.AddHours(24);
        var finExcedido = inicio.AddHours(24).AddMinutes(1);

        // Exactamente 24h es válido
        var reserva24h = _service.CrearReserva("FUE-01", "Solicitante 24h", inicio, finExacto24h);
        Assert.Equal(EstadoReserva.Activa, reserva24h.Estado);

        // Más de 24h es rechazado
        Assert.Throws<ArgumentException>(() =>
            _service.CrearReserva("MIC-01", "Solicitante Excedido", inicio, finExcedido));
    }

    // 6. Cancelación no destructiva (conservación de registro e ID + liberación horaria)
    [Fact]
    public void CancelarReserva_ConservaRegistroHistoricoYLiberaIntervaloHorario()
    {
        var inicio = new DateTime(2026, 11, 25, 10, 0, 0);
        var fin = new DateTime(2026, 11, 25, 12, 0, 0);

        var original = _service.CrearReserva("MIC-01", "Dr. Original", inicio, fin);
        Assert.False(_service.ConsultarDisponibilidad("MIC-01", inicio, fin));

        var cancelada = _service.CancelarReserva(original.Id);

        // Verifica que los datos originales se conservan intactos con estado Cancelada
        Assert.Equal(original.Id, cancelada.Id);
        Assert.Equal("MIC-01", cancelada.UnidadId);
        Assert.Equal("Dr. Original", cancelada.Solicitante);
        Assert.Equal(inicio, cancelada.Inicio);
        Assert.Equal(fin, cancelada.Fin);
        Assert.Equal(EstadoReserva.Cancelada, cancelada.Estado);

        // Verifica liberación inmediata del turno
        Assert.True(_service.ConsultarDisponibilidad("MIC-01", inicio, fin));

        // Permite una nueva reserva en el mismo horario
        var reemplazo = _service.CrearReserva("MIC-01", "Dra. Reemplazo", inicio, fin);
        Assert.NotEqual(original.Id, reemplazo.Id);

        // Ambas reservas (la cancelada y la activa) permanecen en el historial
        var historial = _service.ObtenerHistorial("MIC-01");
        Assert.Equal(2, historial.Count);
        Assert.Contains(historial, r => r.Id == original.Id && r.Estado == EstadoReserva.Cancelada);
        Assert.Contains(historial, r => r.Id == reemplazo.Id && r.Estado == EstadoReserva.Activa);
    }

    // 7. Persistencia tras reinicio
    [Fact]
    public void PersistenciaTrasReinicio_RecuperaReservasActivasYCanceladasIntactas()
    {
        var inicio1 = new DateTime(2026, 12, 1, 8, 0, 0);
        var fin1 = new DateTime(2026, 12, 1, 10, 0, 0);
        var inicio2 = new DateTime(2026, 12, 1, 14, 0, 0);
        var fin2 = new DateTime(2026, 12, 1, 16, 0, 0);

        var reservaActiva = _service.CrearReserva("OSC-01", "Usuario Activo", inicio1, fin1);
        var reservaACancelar = _service.CrearReserva("MUL-01", "Usuario Cancelado", inicio2, fin2);
        _service.CancelarReserva(reservaACancelar.Id);

        // Simular cierre y reinicio completo instanciando nuevo repositorio y servicio sobre el mismo archivo
        var nuevoRepo = new ReservaRepository(_tempFile);
        var nuevoService = new ReservaService(nuevoRepo, Inventario.Default);

        var historialRecuperado = nuevoService.ObtenerHistorial();
        Assert.Equal(2, historialRecuperado.Count);

        var recuperadaActiva = historialRecuperado.Single(r => r.Id == reservaActiva.Id);
        Assert.Equal("OSC-01", recuperadaActiva.UnidadId);
        Assert.Equal("Usuario Activo", recuperadaActiva.Solicitante);
        Assert.Equal(inicio1, recuperadaActiva.Inicio);
        Assert.Equal(fin1, recuperadaActiva.Fin);
        Assert.Equal(EstadoReserva.Activa, recuperadaActiva.Estado);

        var recuperadaCancelada = historialRecuperado.Single(r => r.Id == reservaACancelar.Id);
        Assert.Equal("MUL-01", recuperadaCancelada.UnidadId);
        Assert.Equal("Usuario Cancelado", recuperadaCancelada.Solicitante);
        Assert.Equal(inicio2, recuperadaCancelada.Inicio);
        Assert.Equal(fin2, recuperadaCancelada.Fin);
        Assert.Equal(EstadoReserva.Cancelada, recuperadaCancelada.Estado);

        // Verificar que la reserva activa sigue bloqueando su intervalo tras el reinicio y la cancelada no
        Assert.False(nuevoService.ConsultarDisponibilidad("OSC-01", inicio1, fin1));
        Assert.True(nuevoService.ConsultarDisponibilidad("MUL-01", inicio2, fin2));
    }
}
