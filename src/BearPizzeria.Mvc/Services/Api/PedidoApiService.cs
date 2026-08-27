using System.Net.Http.Headers;
using System.Net.Http.Json;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Services.Api;

public class PedidoApiService : IPedidoApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PedidoApiService> _logger;

    public PedidoApiService(HttpClient httpClient, ILogger<PedidoApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PedidoDto?> GetPedidoByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<PedidoDto>($"/api/pedidos/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar pedido #{Id}", id);
            return null;
        }
    }

    public async Task<PedidoDto?> GetMiPedidoActivoAsync(string? token = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/pedidos/mi-pedido-activo");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PedidoDto>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar mi pedido activo");
            return null;
        }
    }

    public async Task<List<PedidoDto>> GetPedidosByClienteAsync(int clienteId, string? token = null)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/pedidos/cliente/{clienteId}");
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<PedidoDto>>();
                return result ?? [];
            }
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar historial de pedidos para cliente {ClienteId}", clienteId);
            return [];
        }
    }
}
