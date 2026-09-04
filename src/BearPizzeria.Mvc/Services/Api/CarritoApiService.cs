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

    public async Task<CarritoDto?> GetCarritoAsync(int clienteId, string? token = null)
    {
        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/carrito/{clienteId}");
            if (!string.IsNullOrEmpty(token))
                reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(reqMsg);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CarritoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener carrito para cliente {ClienteId}", clienteId);
            return null;
        }
    }

    public async Task<CarritoDto?> AgregarItemCarritoAsync(int clienteId, AgregarItemCarritoDto request, string? token = null)
    {
        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/carrito/{clienteId}/items");
            reqMsg.Content = JsonContent.Create(request);
            if (!string.IsNullOrEmpty(token))
                reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(reqMsg);
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

    public async Task<CarritoDto?> ActualizarItemCarritoAsync(int itemId, ActualizarItemCarritoDto request, string? token = null)
    {
        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/carrito/items/{itemId}");
            reqMsg.Content = JsonContent.Create(request);
            if (!string.IsNullOrEmpty(token))
                reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(reqMsg);
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

    public async Task<CarritoDto?> EliminarItemCarritoAsync(int itemId, string? token = null)
    {
        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/carrito/items/{itemId}");
            if (!string.IsNullOrEmpty(token))
                reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(reqMsg);
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

    public async Task<CarritoDto?> VaciarCarritoAsync(int clienteId, string? token = null)
    {
        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/carrito/{clienteId}/vaciar");
            if (!string.IsNullOrEmpty(token))
                reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(reqMsg);
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

public async Task<PedidoDto?> CheckoutCarritoAsync(int clienteId, int? direccionId = null, string? token = null)
    {
        try
        {
            using var reqMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/carrito/{clienteId}/checkout")
            {
                Content = JsonContent.Create(new { direccionId })
            };
            if (!string.IsNullOrEmpty(token))
                reqMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(reqMsg);
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
