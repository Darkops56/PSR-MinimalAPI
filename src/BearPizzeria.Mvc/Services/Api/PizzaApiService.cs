using System.Net.Http.Json;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Services.Api;

public class PizzaApiService : IPizzaApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PizzaApiService> _logger;

    public PizzaApiService(HttpClient httpClient, ILogger<PizzaApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<PizzaDto>> GetPizzasAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<PizzaDto>>("/api/pizzas");
            return result ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener catálogo de pizzas desde la API");
            return [];
        }
    }

    public async Task<PizzaDto?> GetPizzaByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PizzaDto>($"/api/pizzas/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pizza con ID {Id}", id);
            return null;
        }
    }
}
