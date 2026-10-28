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

public class PedidosYRepetirEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PedidosYRepetirEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(int ClienteId, string Token)> RegistrarUsuarioAsync()
    {
        var username = $"user_{Guid.NewGuid():N}"[..12];
        var reg = new RegisterRequest(Nombre: "Cliente Pedidos", Telefono: "+54 11 1234-5678", Email: $"{username}@test.com", Username: username, Password: "password123", Direccion: "Direccion 123");
        var res = await _client.PostAsJsonAsync("/api/auth/register", reg);
        res.EnsureSuccessStatusCode();
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>();
        return (auth!.ClienteId, auth.Token);
    }

    private async Task<PedidoResponse> CrearPedidoDePruebaAsync(int clienteId, string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var addRes = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/items", new AgregarCarritoItemRequest(3, "Grande", 1));
        addRes.EnsureSuccessStatusCode();

        var res = await _client.PostAsJsonAsync($"/api/carrito/{clienteId}/checkout", new CheckoutRequest(null, "Direccion 123"));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<PedidoResponse>())!;
    }

    [Fact]
    public async Task TransicionEstados_CU14_AvanzaCicloCorrectamente()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        var pedido = await CrearPedidoDePruebaAsync(clienteId, token);
        Assert.Equal("EnPreparacion", pedido.Estado);

        // Avanzar a EnViaje
        var patch1 = await _client.PatchAsync($"/api/pedidos/{pedido.Id}/estado", null);
        Assert.Equal(HttpStatusCode.OK, patch1.StatusCode);
        var ped1 = await patch1.Content.ReadFromJsonAsync<PedidoResponse>();
        Assert.Equal("EnViaje", ped1!.Estado);

        // Avanzar a Entregado
        var patch2 = await _client.PatchAsync($"/api/pedidos/{pedido.Id}/estado", null);
        Assert.Equal(HttpStatusCode.OK, patch2.StatusCode);
        var ped2 = await patch2.Content.ReadFromJsonAsync<PedidoResponse>();
        Assert.Equal("Entregado", ped2!.Estado);

        // Intentar avanzar cuando ya está entregado (Sad Path)
        var patch3 = await _client.PatchAsync($"/api/pedidos/{pedido.Id}/estado", null);
        Assert.Equal(HttpStatusCode.BadRequest, patch3.StatusCode);
        var error = await patch3.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("ESTADO-400", error.Codigo);
        Assert.Contains("ya fue entregado. No se puede avanzar", error.Mensaje);
    }

    [Fact]
    public async Task MonitorearPedidosActivos_CU17_RetornaPedidosEnProgreso()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        var pedido = await CrearPedidoDePruebaAsync(clienteId, token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync("/api/pedidos/mis-pedidos-activos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var activos = await response.Content.ReadFromJsonAsync<List<PedidoResponse>>();

        Assert.NotNull(activos);
        Assert.Contains(activos, p => p.Id == pedido.Id);
    }

    [Fact]
    public async Task HistorialPedidos_CU18_RetornaListaDePedidosDelCliente()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        var pedido = await CrearPedidoDePruebaAsync(clienteId, token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/pedidos/cliente/{clienteId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pedidos = await response.Content.ReadFromJsonAsync<List<PedidoResponse>>();

        Assert.NotNull(pedidos);
        Assert.Contains(pedidos, p => p.Id == pedido.Id);
    }

    [Fact]
    public async Task RepetirPedido_CU19_ConStockCompleto_AgregaTodosLosItems()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        var pedido = await CrearPedidoDePruebaAsync(clienteId, token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsync($"/api/pedidos/{pedido.Id}/repetir?clienteId={clienteId}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var repetirRes = await response.Content.ReadFromJsonAsync<RepetirPedidoResponse>();

        Assert.NotNull(repetirRes);
        Assert.True(repetirRes.TotalmenteAgregado);
        Assert.Equal("¡Se agregaron todas las pizzas de tu pedido anterior al carrito!", repetirRes.Mensaje);
        Assert.Empty(repetirRes.ItemsSinStockSuficiente);
        Assert.NotEmpty(repetirRes.CarritoActualizado.Items);
    }

    [Fact]
    public async Task RepetirPedido_CU20_ConStockParcial_AgregaRemanenteYAlertaAlCliente()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        var pedido = await CrearPedidoDePruebaAsync(clienteId, token);

        int stockOriginal = 0;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            var pizza = db.Pizzas.Find(3);
            stockOriginal = pizza!.Stock;
            pizza.Stock = 0;
            await db.SaveChangesAsync();
        }

        try
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.PostAsync($"/api/pedidos/{pedido.Id}/repetir?clienteId={clienteId}", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var repetirRes = await response.Content.ReadFromJsonAsync<RepetirPedidoResponse>();

            Assert.NotNull(repetirRes);
            Assert.False(repetirRes.TotalmenteAgregado);
            Assert.Contains("Algunas pizzas de tu pedido anterior no tenían suficiente stock", repetirRes.Mensaje);
            Assert.NotEmpty(repetirRes.ItemsSinStockSuficiente);
        }
        finally
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            var pizza = db.Pizzas.Find(3);
            if (pizza != null)
            {
                pizza.Stock = stockOriginal;
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task RepetirPedido_CU19_ConPedidoAjenoOInexistente_DebeFallarCon404()
    {
        var (clienteId, token) = await RegistrarUsuarioAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        const int idInexistente = 88888;
        var response = await _client.PostAsync($"/api/pedidos/{idInexistente}/repetir?clienteId={clienteId}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("PEDIDO-404", error.Codigo);
        Assert.Equal("Pedido no encontrado o no pertenece a este cliente.", error.Mensaje);
    }
}
