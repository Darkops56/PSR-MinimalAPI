using BearPizzeria.Api.Models;
using Xunit;

namespace BearPizzeria.Api.Tests.Services;

public class CalculoPreciosTests
{
    [Theory]
    [InlineData(5000, TamanoPizza.Personal, 3500.00)]   // 5000 * 0.70 = 3500
    [InlineData(5000, TamanoPizza.Mediana, 4250.00)]    // 5000 * 0.85 = 4250
    [InlineData(5000, TamanoPizza.Grande, 5000.00)]     // 5000 * 1.00 = 5000
    [InlineData(5000, TamanoPizza.Familiar, 6500.00)]   // 5000 * 1.30 = 6500
    public void CalcularPrecioPorTamano_AplicaMultiplicadorExacto(decimal precioBase, TamanoPizza tamano, decimal precioEsperado)
    {
        var resultado = BearPizzeria.Api.CalculosPizzeria.CalcularPrecioPorTamano(precioBase, tamano);
        Assert.Equal(precioEsperado, resultado);
    }
}
