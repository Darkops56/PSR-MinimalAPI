using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BearPizzeria.Api.Data;
using BearPizzeria.Api.Models;
using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BearPizzeria.Api.Tests.Integration;

public class CheckoutEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CheckoutEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(int ClienteId, string Token)> RegistrarUsuarioAsync(string? direccion = "Calle Principal 123")
    {
        var username = $"user_{Guid.NewGuid():N}"[..12];
        var reg = new RegisterRequest("Cliente Checkout", "+54 11 1234-5678", $"{username}@test.com", username, "pass123", direccion ?? "");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.ClienteId, auth.Token);
    }

    [Fact]
    public async Task Checkout_CU10_ConItemsYDireccion_CreaPedidoDescuentaStockYVaciaCarrito()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Consultar stock inicial de pizza 1 (Muzzarella)
        var pizzaInicial = await _client.GetFromJsonAsync<PizzaResponse>("/api/pizzas/1");
        var stockInicial = pizzaInicial!.Stock;

        // Agregar 2 unidades al carrito
        await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", new AgregarCarritoItemRequest(1, "Grande", 2));

        // Checkout
        var checkoutReq = new CheckoutRequest(null, "Direccion de prueba 123");
        var response = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/checkout", checkoutReq);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var pedido = await response.Content.ReadFromJsonAsync<PedidoResponse>();

        Assert.NotNull(pedido);
        Assert.Equal("EnPreparacion", pedido.Estado);
        Assert.Equal(clienteId, pedido.ClienteId);
        Assert.Single(pedido.Items);

        // Verificar que el stock se descontó
        var pizzaFinal = await _client.GetFromJsonAsync<PizzaResponse>("/api/pizzas/1");
        Assert.Equal(stockInicial - 2, pizzaFinal!.Stock);

        // Verificar que el carrito quedó vacío
        var carritoRes = await _client.GetAsync($"/api/carrito/{clienteId}");
        var carrito = await carritoRes.Content.ReadFromJsonAsync<CarritoResponse>();
        Assert.Empty(carrito!.Items);
        Assert.Equal(0.00m, carrito.Total);
    }

    [Fact]
    public async Task Checkout_CU10_ConCarritoVacio_DebeFallarConMensajeEntendible()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var checkoutReq = new CheckoutRequest(null, "Direccion 123");
        var response = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/checkout", checkoutReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("CHECKOUT-400", error.Codigo);
        Assert.Equal("El carrito está vacío. Agregá pizzas antes de confirmar.", error.Mensaje);
    }

    [Fact]
    public async Task Checkout_CU10_CU11_SinDireccion_DebeFallarConMensajeEntendible()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync(direccion: "");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            var dirs = db.DireccionesCliente.Where(d => d.ClienteId == clienteId);
            db.DireccionesCliente.RemoveRange(dirs);
            var cliente = db.Clientes.Find(clienteId);
            if (cliente != null) cliente.Direccion = "";
            await db.SaveChangesAsync();
        }

        // Agregar 1 pizza
        await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", new AgregarCarritoItemRequest(1, "Grande", 1));

        // Checkout sin direccion
        var checkoutReq = new CheckoutRequest(null, "");
        var response = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/checkout", checkoutReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("DIRECCION-400", error.Codigo);
        Assert.Contains("Debés seleccionar o ingresar una dirección de entrega antes de confirmar tu pedido.", error.Mensaje);
    }

    [Fact]
    public async Task Checkout_CU10_CU09_ConStockInsuficienteAlConfirmar_DebeHacerRollbackTransaccional()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Agregar 2 pizzas al carrito
        await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", new AgregarCarritoItemRequest(2, "Grande", 2));

        // Simular que justo antes del checkout otra compra reduce el stock
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            var pizza = db.Pizzas.Find(2);
            pizza!.Stock = 1;
            await db.SaveChangesAsync();
        }

        var checkoutReq = new CheckoutRequest(null, "Direccion 123");
        var response = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/checkout", checkoutReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("STOCK-400", error.Codigo);
        Assert.Contains("Stock insuficiente para la pizza", error.Mensaje);

        // Verificar que el stock no quedó negativo
        var pizzaFinal = await _client.GetFromJsonAsync<PizzaResponse>("/api/pizzas/2");
        Assert.Equal(1, pizzaFinal!.Stock);

        // Verificar que no se creó pedido
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            var pedidos = db.Pedidos.Where(p => p.ClienteId == clienteId).ToList();
            Assert.Empty(pedidos);
        }
    }
}
