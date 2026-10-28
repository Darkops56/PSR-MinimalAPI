using System.Net;
using System.Net.Http.Json;
using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Common.Testing;
using Xunit;

namespace BearPizzeria.Api.Tests.Integration;

public class CatalogoEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CatalogoEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPizzas_CU01_RetornaCatalogoConPizzasActivas()
    {
        var response = await _client.GetAsync("/api/pizzas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pizzas = await response.Content.ReadFromJsonAsync<List<PizzaResponse>>();

        Assert.NotNull(pizzas);
        Assert.NotEmpty(pizzas);
        Assert.Contains(pizzas, p => p.Nombre == "Muzzarella");
        Assert.All(pizzas, p =>
        {
            Assert.True(p.Precio > 0);
            Assert.True(p.Stock >= 0);
        });
    }

    [Fact]
    public async Task GetPizzaPorId_CU01_ConIdExistente_RetornaPizzaCorrecta()
    {
        var response = await _client.GetAsync("/api/pizzas/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pizza = await response.Content.ReadFromJsonAsync<PizzaResponse>();

        Assert.NotNull(pizza);
        Assert.Equal(1, pizza.Id);
        Assert.Equal("Muzzarella", pizza.Nombre);
    }

    [Fact]
    public async Task GetPizzaPorId_CU01_ConIdInexistente_DebeFallarCon404YMensajeEntendible()
    {
        const int idInexistente = 99999;
        var response = await _client.GetAsync($"/api/pizzas/{idInexistente}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("PIZZA-404", error.Codigo);
        Assert.Contains($"Pizza con ID {idInexistente} no encontrada", error.Mensaje);
    }
}

