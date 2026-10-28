using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using BearPizzeria.Mvc.Controllers;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Tests.Fakes;
using Xunit;

namespace BearPizzeria.Mvc.Tests.Controllers;

public class CartControllerTests
{
    private readonly FakeCarritoApiService _fakeCarritoService = new();
    private readonly CartController _controller;

    public CartControllerTests()
    {
        _controller = new CartController(_fakeCarritoService, NullLogger<CartController>.Instance);
        _controller.SetupTestContext();
    }

    [Fact]
    public async Task Index_UsuarioNoAutenticado_RetornaVistaSinCargarCarrito()
    {
        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CartViewModel>(viewResult.Model);
        Assert.False(model.IsAuthenticated);
        Assert.Null(model.Carrito);
    }

    [Fact]
    public async Task Index_UsuarioAutenticado_CargaCarritoYDatosDeUsuario()
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(7, "cliente_carrito"));
        _fakeCarritoService.GetCarritoResult = new CarritoDto(1, 7, DateTime.UtcNow, 4500m, 1, new List<CarritoItemDto>());

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CartViewModel>(viewResult.Model);
        Assert.True(model.IsAuthenticated);
        Assert.NotNull(model.CurrentUser);
        Assert.Equal(7, model.CurrentUser.ClienteId);
        Assert.NotNull(model.Carrito);
        Assert.Equal(4500m, model.Carrito.Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Checkout_SinDireccion_DefineMensajeEntendibleYRedirigeACarrito(int? direccionId)
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));

        var result = await _controller.Checkout(direccionId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.True(_controller.TempData.ContainsKey("ErrorMessage"));
        Assert.Equal("Debés seleccionar o agregar una dirección de entrega antes de confirmar tu pedido.",
            (string)_controller.TempData["ErrorMessage"]!);
    }

    [Fact]
    public async Task Checkout_FallaEnApi_DefineMensajeEntendibleYRedirigeACarrito()
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));
        _fakeCarritoService.CheckoutCarritoResult = null; // Simula error (stock insuficiente, etc.)

        var result = await _controller.Checkout(10);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.True(_controller.TempData.ContainsKey("ErrorMessage"));
        Assert.Equal("No se pudo procesar el pedido. Asegurate de tener pizzas en el carrito y una dirección de entrega válida.",
            (string)_controller.TempData["ErrorMessage"]!);
    }

    [Fact]
    public async Task Checkout_Exitoso_DefineMensajeDeExitoYRedirigeAPerfil()
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));
        _fakeCarritoService.CheckoutCarritoResult = new PedidoDto(99, 1, "user", DateTime.UtcNow, "EnPreparacion", 9000m, "Calle 1", new List<PedidoPizzaDto>());

        var result = await _controller.Checkout(10);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Profile", redirect.ControllerName);

        Assert.True(_controller.TempData.ContainsKey("SuccessMessage"));
        Assert.Equal("¡Pedido #99 confirmado con éxito!", (string)_controller.TempData["SuccessMessage"]!);
    }
}

