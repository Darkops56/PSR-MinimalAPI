using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using BearPizzeria.Mvc.Controllers;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Tests.Fakes;
using Xunit;

namespace BearPizzeria.Mvc.Tests.Controllers;

public class ProfileYHomeControllerTests
{
    private readonly FakePedidoApiService _fakePedidoService = new();
    private readonly FakeUsuarioApiService _fakeUsuarioService = new();
    private readonly FakePizzaApiService _fakePizzaService = new();

    [Fact]
    public async Task HomeController_Index_SeparaPizzasDestacadasYRetornaModelo()
    {
        _fakePizzaService.GetPizzasResult = new List<PizzaDto>
        {
            new(1, "Pizza 1", "Desc 1", 1000m, "Grande", 5),
            new(2, "Pizza 2", "Desc 2", 2000m, "Grande", 5),
            new(3, "Pizza 3", "Desc 3", 3000m, "Grande", 5),
            new(4, "Pizza 4", "Desc 4", 4000m, "Grande", 5),
            new(5, "Pizza 5", "Desc 5", 5000m, "Grande", 5)
        };

        var controller = new HomeController(_fakePizzaService, NullLogger<HomeController>.Instance);
        controller.SetupTestContext();

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeViewModel>(viewResult.Model);

        Assert.Equal(5, model.Pizzas.Count);
        Assert.Equal(3, model.FeaturedPizzas.Count);
        Assert.False(model.IsAuthenticated);
    }

    [Fact]
    public async Task ProfileController_Orders_PaginacionCorrecta()
    {
        // Generar 25 pedidos para probar paginación de 10
        var pedidos = Enumerable.Range(1, 25).Select(i =>
            new PedidoDto(i, 1, "user", DateTime.UtcNow, "Entregado", 1000m * i, "Dir", new List<PedidoPizzaDto>())
        ).ToList();

        _fakePedidoService.GetPedidosByClienteResult = pedidos;

        var controller = new ProfileController(_fakePedidoService, _fakeUsuarioService, NullLogger<ProfileController>.Instance);
        controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));

        // Página 1 (omite 3 del perfil principal -> 22 restantes -> página 1 toma 10)
        var result = await controller.Orders(page: 1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<OrdersViewModel>(viewResult.Model);

        Assert.Equal(1, model.PageNumber);
        Assert.Equal(10, model.Pedidos.Count);
        Assert.Equal(3, model.TotalPages);
    }

    [Fact]
    public async Task ProfileController_UpdateProfile_ModelStateInvalido_DefineMensajeEntendible()
    {
        var controller = new ProfileController(_fakePedidoService, _fakeUsuarioService, NullLogger<ProfileController>.Instance);
        controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));
        controller.ModelState.AddModelError("Email", "El email es obligatorio");

        var model = new EditProfileViewModel();
        var result = await controller.UpdateProfile(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
        Assert.Equal("Por favor corregí los errores en el formulario de perfil.", (string)controller.TempData["ErrorMessage"]!);
    }

    [Fact]
    public async Task ProfileController_UpdateProfile_ApiRechaza_DefineMensajeEntendible()
    {
        var controller = new ProfileController(_fakePedidoService, _fakeUsuarioService, NullLogger<ProfileController>.Instance);
        controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));

        _fakeUsuarioService.UpdateClienteResult = null; // Simula error (email/username ya en uso)

        var model = new EditProfileViewModel
        {
            Username = "usuario_en_uso",
            Nombre = "Juan",
            Email = "ya_existe@correo.com",
            Telefono = "+54 11 0000-0000"
        };

        var result = await controller.UpdateProfile(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
        Assert.Equal("No se pudieron actualizar los datos. Verificá si el usuario o email ya pertenecen a otra cuenta.",
            (string)controller.TempData["ErrorMessage"]!);
    }

    [Fact]
    public async Task ProfileController_UpdateProfile_Exitoso_DefineMensajeDeExito()
    {
        var controller = new ProfileController(_fakePedidoService, _fakeUsuarioService, NullLogger<ProfileController>.Instance);
        controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));

        _fakeUsuarioService.UpdateClienteResult = new ClienteDto(1, "nuevo_user", "Nuevo Nombre", "Nueva Dir", "+54 11 9999-9999", "nuevo@correo.com");

        var model = new EditProfileViewModel
        {
            Username = "nuevo_user",
            Nombre = "Nuevo Nombre",
            Direccion = "Nueva Dir",
            Email = "nuevo@correo.com",
            Telefono = "+54 11 9999-9999"
        };

        var result = await controller.UpdateProfile(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        Assert.Equal("¡Perfil y usuario actualizados con éxito!", (string)controller.TempData["SuccessMessage"]!);
    }
}

