using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Common.Testing;
using Xunit;

namespace BearPizzeria.Api.Tests.Integration;

public class CarritoYStockEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CarritoYStockEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(int ClienteId, string Token)> RegistrarUsuarioAsync()
    {
        var username = $"user_{Guid.NewGuid():N}"[..12];
        var reg = new RegisterRequest(Nombre: "User", Telefono: "+54 11 0000-0000", Email: $"{username}@test.com", Username: username, Password: "password123", Direccion: "Direccion 1");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.ClienteId, auth.Token);
    }

    [Fact]
    public async Task AgregarItem_CU07_ConStockSuficiente_CalculaSubtotalYTotal()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Agregar 2 pizzas Grandes (precio base = 4500, multiplicador Grande = 1.00 -> 9000)
        var agregarReq = new AgregarCarritoItemRequest(1, "Grande", 2);
        var response = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", agregarReq);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var carrito = await response.Content.ReadFromJsonAsync<CarritoResponse>();

        Assert.NotNull(carrito);
        Assert.Single(carrito.Items);
        var item = carrito.Items[0];
        Assert.Equal(1, item.PizzaId);
        Assert.Equal(2, item.Cantidad);
        Assert.Equal(4500.00m, item.PrecioUnitario);
        Assert.Equal(9000.00m, item.Subtotal);
        Assert.Equal(9000.00m, carrito.Total);
    }

    [Fact]
    public async Task AgregarItem_CU07_CU09_SuperandoStockDisponible_DebeFallarConMensajeEntendible()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Pizza 5 (Calabresa) tiene stock inicial = 2
        var agregarReq = new AgregarCarritoItemRequest(5, "Grande", 999);
        var response = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", agregarReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("STOCK-400", error.Codigo);
        Assert.Contains("Stock insuficiente para Calabresa", error.Mensaje);
        Assert.Contains("unidades en stock", error.Mensaje);
    }

    [Fact]
    public async Task ModificarItem_CU08_CantidadValida_ActualizaSubtotalYTotal()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var agregarReq = new AgregarCarritoItemRequest(1, "Grande", 1);
        var addRes = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", agregarReq);
        var carrito = await addRes.Content.ReadFromJsonAsync<CarritoResponse>();
        var itemId = carrito!.Items[0].Id;

        var updateReq = new ActualizarCarritoItemRequest(3);
        var updateRes = await _client.PutAsJsonAsync($"/api/carrito/items/{itemId}", updateReq);

        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
        var carritoActualizado = await updateRes.Content.ReadFromJsonAsync<CarritoResponse>();

        Assert.NotNull(carritoActualizado);
        var itemModificado = carritoActualizado.Items.First(i => i.Id == itemId);
        Assert.Equal(3, itemModificado.Cantidad);
        Assert.Equal(13500.00m, itemModificado.Subtotal);
        Assert.Equal(13500.00m, carritoActualizado.Total);
    }

    [Fact]
    public async Task ModificarItem_CU08_SuperandoStock_DebeFallarConMensajeEntendible()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Calabresa (Id 5) stock = 2
        var agregarReq = new AgregarCarritoItemRequest(5, "Grande", 1);
        var addRes = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", agregarReq);
        var carrito = await addRes.Content.ReadFromJsonAsync<CarritoResponse>();
        var itemId = carrito!.Items[0].Id;

        var updateReq = new ActualizarCarritoItemRequest(50);
        var updateRes = await _client.PutAsJsonAsync($"/api/carrito/items/{itemId}", updateReq);

        Assert.Equal(HttpStatusCode.BadRequest, updateRes.StatusCode);
        var error = await updateRes.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("STOCK-400", error.Codigo);
        Assert.Contains("Stock insuficiente para Calabresa", error.Mensaje);
    }

    [Fact]
    public async Task ModificarItem_CU08_ItemInexistente_DebeFallarCon404YMensajeEntendible()
    {
        var (_, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const int itemIdInexistente = 99999;
        var updateReq = new ActualizarCarritoItemRequest(2);
        var updateRes = await _client.PutAsJsonAsync($"/api/carrito/items/{itemIdInexistente}", updateReq);

        Assert.Equal(HttpStatusCode.NotFound, updateRes.StatusCode);
        var error = await updateRes.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("CARRITO-404", error.Codigo);
        Assert.Equal("Item de carrito no encontrado.", error.Mensaje);
    }

    [Fact]
    public async Task EliminarItemYVaciarCarrito_CU08_LimpiaTotalACero()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", new AgregarCarritoItemRequest(1, "Grande", 1));
        await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", new AgregarCarritoItemRequest(2, "Grande", 1));

        // Vaciar carrito
        var vaciarRes = await _client.DeleteAsync($"/api/carrito/{clienteId}/vaciar");
        Assert.Equal(HttpStatusCode.OK, vaciarRes.StatusCode);

        var carritoVacio = await vaciarRes.Content.ReadFromJsonAsync<CarritoResponse>();
        Assert.NotNull(carritoVacio);
        Assert.Empty(carritoVacio.Items);
        Assert.Equal(0.00m, carritoVacio.Total);
    }

    [Fact]
    public async Task Carrito_SinTokenOConTokenAjeno_DebeRetornar401Unauthorized()
    {
        var (clienteId, _) = await RegistrarUsuarioAsync();
        var (_, tokenAjeno) = await RegistrarUsuarioAsync();

        _client.DefaultRequestHeaders.Authorization = null;
        var resSinToken = await _client.GetAsync($"/api/carrito/{clienteId}");
        Assert.Equal(HttpStatusCode.Unauthorized, resSinToken.StatusCode);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenAjeno);
        var resTokenAjeno = await _client.GetAsync($"/api/carrito/{clienteId}");
        Assert.Equal(HttpStatusCode.Unauthorized, resTokenAjeno.StatusCode);
    }
}
