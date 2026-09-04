using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Services.Interfaces;

public interface ICarritoApiService
{
    Task<CarritoDto?> GetCarritoAsync(int clienteId, string? token = null);
    Task<CarritoDto?> AgregarItemCarritoAsync(int clienteId, AgregarItemCarritoDto request, string? token = null);
    Task<CarritoDto?> ActualizarItemCarritoAsync(int itemId, ActualizarItemCarritoDto request, string? token = null);
    Task<CarritoDto?> EliminarItemCarritoAsync(int itemId, string? token = null);
    Task<CarritoDto?> VaciarCarritoAsync(int clienteId, string? token = null);
    Task<PedidoDto?> CheckoutCarritoAsync(int clienteId, int? direccionId = null, string? token = null);
}
