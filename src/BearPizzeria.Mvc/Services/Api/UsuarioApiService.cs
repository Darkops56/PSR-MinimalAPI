using System.Net.Http.Json;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Services.Api;

public class UsuarioApiService : IUsuarioApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UsuarioApiService> _logger;

    public UsuarioApiService(HttpClient httpClient, ILogger<UsuarioApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            }
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Error en registro: {Error}", error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción en registro de usuario");
            return null;
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            }
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Error en login: {Error}", error);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción en login de usuario");
            return null;
        }
    }

    public async Task<AuthResponseDto?> GetUserSessionAsync(int clienteId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AuthResponseDto>($"/api/auth/me/{clienteId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesión del cliente {ClienteId}", clienteId);
            return null;
        }
    }
}
