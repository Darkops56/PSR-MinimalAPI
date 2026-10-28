using System.Net;
using System.Text.Json;
using BearPizzeria.Common.Testing;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BearPizzeria.Mvc.Tests.Services;

public class ApiServicesTests
{
    [Fact]
    public async Task UsuarioApiService_RegisterAsync_Exito_RetornaAuthResponse()
    {
        var authDto = new AuthResponseDto(1, "juan", "Juan Perez", "juan@correo.com", "Dir 123", "+54 11 0000", "token123");
        var json = JsonSerializer.Serialize(authDto);

        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new UsuarioApiService(httpClient, NullLogger<UsuarioApiService>.Instance);

        var request = new RegisterDto("Juan Perez", "+54 11 0000", "juan@correo.com", "juan", "pass123", "Dir 123");
        var result = await service.RegisterAsync(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.ClienteId);
        Assert.Equal("token123", result.Token);
    }

    [Fact]
    public async Task UsuarioApiService_RegisterAsync_FallaHttp_RetornaNullSinLanzarExcepcion()
    {
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.BadRequest, "{\"codigo\":\"AUTH-400\"}");
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new UsuarioApiService(httpClient, NullLogger<UsuarioApiService>.Instance);

        var request = new RegisterDto("Juan", "111", "email@test.com", "user", "pass", "dir");
        var result = await service.RegisterAsync(request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UsuarioApiService_LoginAsync_CredencialesErroneas_RetornaNull()
    {
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.Unauthorized, "{}");
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new UsuarioApiService(httpClient, NullLogger<UsuarioApiService>.Instance);

        var result = await service.LoginAsync(new LoginDto("user", "wrong_password"));
        Assert.Null(result);
    }

    [Fact]
    public async Task CarritoApiService_GetCarritoAsync_Exito_RetornaCarritoConItems()
    {
        var carritoDto = new CarritoDto(
            1,
            1,
            DateTime.UtcNow,
            9000m,
            2,
            new List<CarritoItemDto>
            {
                new(10, 1, "Muzzarella", "Descripcion", "Grande", 2, 4500m, 9000m)
            }
        );
        var json = JsonSerializer.Serialize(carritoDto);

        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new CarritoApiService(httpClient, NullLogger<CarritoApiService>.Instance);

        var result = await service.GetCarritoAsync(1, "token_valido");

        Assert.NotNull(result);
        Assert.Equal(9000m, result.Total);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task CarritoApiService_CheckoutCarritoAsync_FallaPorStockOError_RetornaNull()
    {
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.BadRequest, "{\"codigo\":\"STOCK-400\"}");
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new CarritoApiService(httpClient, NullLogger<CarritoApiService>.Instance);

        var result = await service.CheckoutCarritoAsync(1, 10, "token_valido");
        Assert.Null(result);
    }

    [Fact]
    public async Task PedidoApiService_GetMisPedidosActivosAsync_Exito_RetornaLista()
    {
        var pedidos = new List<PedidoDto>
        {
            new(1, 1, "user", DateTime.UtcNow, "EnPreparacion", 4500m, "Direccion 123", new List<PedidoPizzaDto>())
        };
        var json = JsonSerializer.Serialize(pedidos);

        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new PedidoApiService(httpClient, NullLogger<PedidoApiService>.Instance);

        var result = await service.GetMisPedidosActivosAsync("token");

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("EnPreparacion", result[0].Estado);
    }

    [Fact]
    public async Task PedidoApiService_GetPedidosByClienteAsync_ErrorDeRed_RetornaListaVacia()
    {
        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "Error interno");
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new PedidoApiService(httpClient, NullLogger<PedidoApiService>.Instance);

        var result = await service.GetPedidosByClienteAsync(1, "token");

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task PizzaApiService_GetPizzasAsync_Exito_RetornaListaDePizzas()
    {
        var pizzas = new List<PizzaDto>
        {
            new(1, "Muzzarella", "Clasica", 4500m, "Grande", 10),
            new(2, "Calabresa", "Picante", 5200m, "Grande", 5)
        };
        var json = JsonSerializer.Serialize(pizzas);

        var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, json);
        var httpClient = new HttpClient(mockHandler) { BaseAddress = new Uri("http://localhost:5250") };
        var service = new PizzaApiService(httpClient, NullLogger<PizzaApiService>.Instance);

        var result = await service.GetPizzasAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}

