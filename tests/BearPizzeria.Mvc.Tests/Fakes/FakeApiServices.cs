using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Tests.Fakes;

public class FakeUsuarioApiService : IUsuarioApiService
{
    public AuthResponseDto? RegisterResult { get; set; }
    public AuthResponseDto? LoginResult { get; set; }
    public AuthResponseDto? GetUserSessionResult { get; set; }
    public ClienteDto? UpdateClienteResult { get; set; }

    public Task<AuthResponseDto?> RegisterAsync(RegisterDto request) => Task.FromResult(RegisterResult);
    public Task<AuthResponseDto?> LoginAsync(LoginDto request) => Task.FromResult(LoginResult);
    public Task<AuthResponseDto?> GetUserSessionAsync(int clienteId, string? token = null) => Task.FromResult(GetUserSessionResult);
    public Task<ClienteDto?> UpdateClienteAsync(int clienteId, ActualizarClienteDto request, string? token = null) => Task.FromResult(UpdateClienteResult);
}

public class FakeCarritoApiService : ICarritoApiService
{
    public CarritoDto? GetCarritoResult { get; set; }
    public CarritoDto? AgregarItemResult { get; set; }
    public CarritoDto? ActualizarItemResult { get; set; }
    public CarritoDto? EliminarItemResult { get; set; }
    public CarritoDto? VaciarCarritoResult { get; set; }
    public PedidoDto? CheckoutCarritoResult { get; set; }

    public Task<CarritoDto?> GetCarritoAsync(int clienteId, string? token = null) => Task.FromResult(GetCarritoResult);
    public Task<CarritoDto?> AgregarItemCarritoAsync(int clienteId, AgregarItemCarritoDto request, string? token = null) => Task.FromResult(AgregarItemResult);
    public Task<CarritoDto?> ActualizarItemCarritoAsync(int itemId, ActualizarItemCarritoDto request, string? token = null) => Task.FromResult(ActualizarItemResult);
    public Task<CarritoDto?> EliminarItemCarritoAsync(int itemId, string? token = null) => Task.FromResult(EliminarItemResult);
    public Task<CarritoDto?> VaciarCarritoAsync(int clienteId, string? token = null) => Task.FromResult(VaciarCarritoResult);
    public Task<PedidoDto?> CheckoutCarritoAsync(int clienteId, int? direccionId = null, string? token = null) => Task.FromResult(CheckoutCarritoResult);
}

public class FakePedidoApiService : IPedidoApiService
{
    public PedidoDto? GetPedidoByIdResult { get; set; }
    public PedidoDto? GetMiPedidoActivoResult { get; set; }
    public List<PedidoDto> GetMisPedidosActivosResult { get; set; } = [];
    public List<PedidoDto> GetPedidosByClienteResult { get; set; } = [];

    public Task<PedidoDto?> GetPedidoByIdAsync(int id) => Task.FromResult(GetPedidoByIdResult);
    public Task<PedidoDto?> GetMiPedidoActivoAsync(string? token = null) => Task.FromResult(GetMiPedidoActivoResult);
    public Task<List<PedidoDto>> GetMisPedidosActivosAsync(string? token = null) => Task.FromResult(GetMisPedidosActivosResult);
    public Task<List<PedidoDto>> GetPedidosByClienteAsync(int clienteId, string? token = null) => Task.FromResult(GetPedidosByClienteResult);
}

public class FakePizzaApiService : IPizzaApiService
{
    public List<PizzaDto> GetPizzasResult { get; set; } = [];
    public PizzaDto? GetPizzaByIdResult { get; set; }

    public Task<List<PizzaDto>> GetPizzasAsync() => Task.FromResult(GetPizzasResult);
    public Task<PizzaDto?> GetPizzaByIdAsync(int id) => Task.FromResult(GetPizzaByIdResult);
}

