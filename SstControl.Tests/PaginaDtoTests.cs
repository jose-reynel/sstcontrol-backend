using SstControl.Aplicacion.DTOs;
using Xunit;

namespace SstControl.Tests;

public class PaginaDtoTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(45, 20, 3)]
    public void TotalPaginas_SeCalculaRedondeandoHaciaArriba(int totalElementos, int tamanioPagina, int totalPaginasEsperado)
    {
        var pagina = new PaginaDto<string>([], 1, tamanioPagina, totalElementos);

        Assert.Equal(totalPaginasEsperado, pagina.TotalPaginas);
    }
}
