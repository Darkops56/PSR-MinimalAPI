using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Services.Interfaces;

public interface IPedidoApiService
{
    Task<PedidoDto?> GetPedidoByIdAsync(int id);
    Task<PedidoDto?> GetMiPedidoActivoAsync(string? token = null);
    Task<List<PedidoDto>> GetPedidosByClienteAsync(int clienteId, string? token = null);
}
