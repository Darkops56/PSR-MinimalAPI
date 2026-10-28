using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using BearPizzeria.Mvc.Controllers;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Tests.Fakes;
using Xunit;

namespace BearPizzeria.Mvc.Tests.Controllers;

public class AuthControllerTests
{
    private readonly FakeUsuarioApiService _fakeService = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_fakeService, NullLogger<AuthController>.Instance);
        _controller.SetupTestContext();
    }

    [Fact]
    public void Login_GET_UsuarioNoAutenticado_MuestraVistaLogin()
    {
        var result = _controller.Login();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<LoginViewModel>(viewResult.Model);
        Assert.Null(model.ReturnUrl);
    }

    [Fact]
    public void Login_GET_UsuarioYaAutenticado_RedirigeAHome()
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1, "autenticado"));

        var result = _controller.Login();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Login_POST_ModelStateInvalido_RetornaVistaSinInvocarServicio()
    {
        _controller.ModelState.AddModelError("Username", "El usuario es obligatorio");
        var model = new LoginViewModel { Username = "", Password = "" };

        var result = await _controller.Login(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task Login_POST_CredencialesErroneas_MuestraMensajeEntendibleEnModelState()
    {
        _fakeService.LoginResult = null; // Simula fallo en API
        var model = new LoginViewModel { Username = "usuario_erroneo", Password = "bad_password" };

        var result = await _controller.Login(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        var error = Assert.Single(_controller.ModelState[string.Empty]!.Errors);
        Assert.Equal("Usuario o contraseña incorrectos.", error.ErrorMessage);
    }

    [Fact]
    public async Task Login_POST_CredencialesValidas_IniciaSesionYRedirigeAHome()
    {
        _fakeService.LoginResult = new AuthResponseDto(10, "juan", "Juan Perez", "juan@test.com", "Dir 123", "+54 11 0000", "token_jwt");
        var model = new LoginViewModel { Username = "juan", Password = "PasswordValido123" };

        var result = await _controller.Login(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void Register_GET_UsuarioNoAutenticado_MuestraVistaRegistro()
    {
        var result = _controller.Register();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<RegisterViewModel>(viewResult.Model);
    }

    [Fact]
    public void Register_GET_UsuarioYaAutenticado_RedirigeAHome()
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1, "autenticado"));

        var result = _controller.Register();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Register_POST_DatosInvalidos_RetornaVista()
    {
        _controller.ModelState.AddModelError("Email", "El email es obligatorio");
        var model = new RegisterViewModel();

        var result = await _controller.Register(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }

    [Fact]
    public async Task Register_POST_UsuarioOEmailDuplicado_MuestraMensajeEntendible()
    {
        _fakeService.RegisterResult = null; // Simula rechazo de la API
        var model = new RegisterViewModel
        {
            Nombre = "Juan",
            Telefono = "+54 11 2222-3333",
            Email = "duplicado@correo.com",
            Username = "duplicado",
            Password = "password123"
        };

        var result = await _controller.Register(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        var error = Assert.Single(_controller.ModelState[string.Empty]!.Errors);
        Assert.Contains("No se pudo completar el registro. El usuario o email pueden ya estar en uso.", error.ErrorMessage);
    }

    [Fact]
    public async Task Register_POST_RegistroExitoso_GuardaMensajeDeExitoYRedirige()
    {
        _fakeService.RegisterResult = new AuthResponseDto(5, "nuevo_user", "Nuevo Usuario", "nuevo@test.com", "Dir 1", "Tel 1", "token123");
        var model = new RegisterViewModel
        {
            Nombre = "Nuevo Usuario",
            Telefono = "+54 11 4444-5555",
            Email = "nuevo@test.com",
            Username = "nuevo_user",
            Password = "password123"
        };

        var result = await _controller.Register(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);

        Assert.True(_controller.TempData.ContainsKey("SuccessMessage"));
        Assert.Contains("¡Bienvenido Nuevo Usuario! Tu cuenta ha sido creada.", (string)_controller.TempData["SuccessMessage"]!);
    }

    [Fact]
    public async Task Logout_CierraSesionYRedirigeAHome()
    {
        _controller.SetupTestContext(user: ControllerTestHelper.CreateUserPrincipal(1));

        var result = await _controller.Logout();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }
}

