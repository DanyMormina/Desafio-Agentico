namespace LabReservations.Tests;

using LabReservations.Cli;

public class InventarioTests
{
    [Fact]
    public void Inventario_ContieneExactamenteCincoUnidadesPredefinidas()
    {
        var inventario = Inventario.Default;

        Assert.Equal(5, inventario.Unidades.Count);
        Assert.Contains(inventario.Unidades, u => u.Id == "MIC-01");
        Assert.Contains(inventario.Unidades, u => u.Id == "MIC-02");
        Assert.Contains(inventario.Unidades, u => u.Id == "OSC-01");
        Assert.Contains(inventario.Unidades, u => u.Id == "MUL-01");
        Assert.Contains(inventario.Unidades, u => u.Id == "FUE-01");
    }

    [Theory]
    [InlineData("MIC-01")]
    [InlineData("mic-01")]
    [InlineData(" MIC-01 ")]
    [InlineData("OSC-01")]
    [InlineData(" osc-01  ")]
    [InlineData("MUL-01")]
    [InlineData("FUE-01")]
    public void Existe_ConIdValidoOConEspacios_RetornaTrue(string unidadId)
    {
        Assert.True(Inventario.Existe(unidadId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("DESCONOCIDO")]
    [InlineData("MIC-99")]
    public void Existe_ConIdInvalidoONoExistente_RetornaFalse(string? unidadId)
    {
        Assert.False(Inventario.Existe(unidadId));
    }

    [Theory]
    [InlineData("MIC-01", "Microscopio digital")]
    [InlineData(" mic-01 ", "Microscopio digital")]
    [InlineData("OSC-01", "Osciloscopio")]
    public void ObtenerPorId_ConIdValido_RetornaUnidadCorrespondiente(string unidadId, string nombreEsperado)
    {
        var unidad = Inventario.ObtenerPorId(unidadId);

        Assert.NotNull(unidad);
        Assert.Equal(nombreEsperado, unidad.Nombre);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NO_EXISTE")]
    public void ObtenerPorId_ConIdInvalidoONoExistente_RetornaNull(string? unidadId)
    {
        var unidad = Inventario.ObtenerPorId(unidadId);

        Assert.Null(unidad);
    }

    [Fact]
    public void Constructor_ConRutaInexistente_AplicaFallbackEnMemoria()
    {
        var inventarioInexistente = new Inventario("ruta/que/no/existe/inventario.json");

        Assert.Equal(5, inventarioInexistente.Unidades.Count);
        Assert.True(inventarioInexistente.ExisteUnidad("MIC-01"));
    }

    [Fact]
    public void Constructor_ConArchivoJsonValido_CargaUnidadesCorrectamente()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            var json = """
            [
                { "Id": "TEST-01", "Nombre": "Unidad de prueba" }
            ]
            """;
            File.WriteAllText(tempFile, json);

            var inventarioCustom = new Inventario(tempFile);

            Assert.Single(inventarioCustom.Unidades);
            Assert.True(inventarioCustom.ExisteUnidad("TEST-01"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
