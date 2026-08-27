using System.Net.Http.Json;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Services.Api;

public class CarritoApiService : ICarritoApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CarritoApiService> _logger;

    public CarritoApiService(HttpClient httpClient, ILogger<CarritoApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CarritoDto?> GetCarritoAsync(int clienteId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CarritoDto>($"/api/carrito/{clienteId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener carrito para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public async Task<CarritoDto?> AgregarItemCarritoAsync(int clienteId, AgregarItemCarritoDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/carrito/{clienteId}/items", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CarritoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar item al carrito en BD");
            return null;
        }
    }

    public async Task<CarritoDto?> ActualizarItemCarritoAsync(int itemId, ActualizarItemCarritoDto request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/carrito/items/{itemId}", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CarritoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar item del carrito {ItemId}", itemId);
            return null;
        }
    }

    public async Task<CarritoDto?> EliminarItemCarritoAsync(int itemId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/carrito/items/{itemId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CarritoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar item del carrito {ItemId}", itemId);
            return null;
        }
    }

    public async Task<CarritoDto?> VaciarCarritoAsync(int clienteId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/carrito/{clienteId}/vaciar");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CarritoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al vaciar carrito del cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public async Task<PedidoDto?> CheckoutCarritoAsync(int clienteId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/carrito/{clienteId}/checkout", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PedidoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al realizar checkout para cliente {ClienteId}", clienteId);
            return null;
        }
    }
}
