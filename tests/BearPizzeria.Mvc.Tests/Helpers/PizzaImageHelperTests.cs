using BearPizzeria.Mvc.Helpers;
using Xunit;

namespace BearPizzeria.Mvc.Tests.Helpers;

public class PizzaImageHelperTests
{
    [Theory]
    [InlineData("Pizza de Calabresa", "/img/pisadecalabresa.webp")]
    [InlineData("calabresa picante", "/img/pisadecalabresa.webp")]
    [InlineData("Fugazzeta rellena", "/img/pisadefugazzeta.webp")]
    [InlineData("Pizza Napolitana clásica", "/img/pisanapolitana.jpg")]
    [InlineData("Especial con morrones", "/img/pisaespecial.jpeg")]
    [InlineData("Muzzarella", "/img/pisademuzzarella.jpg")]
    [InlineData("Pizza Hawaiana Desconocida", "/img/pisademuzzarella.jpg")]
    [InlineData("", "/img/pisademuzzarella.jpg")]
    [InlineData("   ", "/img/pisademuzzarella.jpg")]
    [InlineData(null, "/img/pisademuzzarella.jpg")]
    public void GetImagePath_RetornaRutaCorrectaOFallback(string? nombre, string rutaEsperada)
    {
        var ruta = PizzaImageHelper.GetImagePath(nombre);
        Assert.Equal(rutaEsperada, ruta);
    }
}

