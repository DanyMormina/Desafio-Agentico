namespace LabReservations.Tests;

[Collection("CliConsoleTests")]
public class CliIntegrationTests
{
    private static (int ExitCode, string StdOut, string StdErr) EjecutarCli(params string[] args)
    {
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var outWriter = new StringWriter();
        using var errWriter = new StringWriter();

        try
        {
            Console.SetOut(outWriter);
            Console.SetError(errWriter);

            var exitCode = Program.Main(args);
            return (exitCode, outWriter.ToString(), errWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Main_AyudaGeneral_RetornaCeroYDescribeSubcomandos(string flagAyuda)
    {
        var (exitCode, stdOut, stdErr) = EjecutarCli(flagAyuda);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(stdErr));
        Assert.Contains("inventario", stdOut);
        Assert.Contains("disponibilidad", stdOut);
        Assert.Contains("reservar", stdOut);
        Assert.Contains("cancelar", stdOut);
        Assert.Contains("historial", stdOut);
    }

    [Fact]
    public void Main_SinArgumentosOConParametrosInvalidos_RetornaUnoYEscribeEnConsoleError()
    {
        // Caso 1: Sin argumentos
        var (exitSinArgs, _, errSinArgs) = EjecutarCli();
        Assert.Equal(1, exitSinArgs);
        Assert.False(string.IsNullOrWhiteSpace(errSinArgs));
        Assert.Contains("[ERROR]", errSinArgs);

        // Caso 2: Parámetros incompletos
        var (exitIncompleto, _, errIncompleto) = EjecutarCli("reservar", "--unit", "MIC-01");
        Assert.Equal(1, exitIncompleto);
        Assert.False(string.IsNullOrWhiteSpace(errIncompleto));
        Assert.Contains("--user", errIncompleto);

        // Caso 3: Comando inexistente
        var (exitDesconocido, _, errDesconocido) = EjecutarCli("comando-invalido");
        Assert.Equal(1, exitDesconocido);
        Assert.False(string.IsNullOrWhiteSpace(errDesconocido));
        Assert.Contains("[ERROR]", errDesconocido);
    }

    [Fact]
    public void Main_SubcomandoInventario_RetornaCeroYListaLasCincoUnidadesFijas()
    {
        var (exitCode, stdOut, stdErr) = EjecutarCli("inventario");

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(stdErr));
        Assert.Contains("MIC-01", stdOut);
        Assert.Contains("MIC-02", stdOut);
        Assert.Contains("OSC-01", stdOut);
        Assert.Contains("MUL-01", stdOut);
        Assert.Contains("FUE-01", stdOut);
    }
}
