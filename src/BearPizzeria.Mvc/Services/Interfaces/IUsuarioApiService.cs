using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Services.Interfaces;

public interface IUsuarioApiService
{
    Task<AuthResponseDto?> RegisterAsync(RegisterDto request);
    Task<AuthResponseDto?> LoginAsync(LoginDto request);
    Task<AuthResponseDto?> GetUserSessionAsync(int clienteId);
    Task<ClienteDto?> UpdateClienteAsync(int clienteId, ActualizarClienteDto request, string? token = null);
}
