using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Services.Interfaces;

public interface ICarritoApiService
{
    Task<CarritoDto?> GetCarritoAsync(int clienteId);
    Task<CarritoDto?> AgregarItemCarritoAsync(int clienteId, AgregarItemCarritoDto request);
    Task<CarritoDto?> ActualizarItemCarritoAsync(int itemId, ActualizarItemCarritoDto request);
    Task<CarritoDto?> EliminarItemCarritoAsync(int itemId);
    Task<CarritoDto?> VaciarCarritoAsync(int clienteId);
    Task<PedidoDto?> CheckoutCarritoAsync(int clienteId);
}
