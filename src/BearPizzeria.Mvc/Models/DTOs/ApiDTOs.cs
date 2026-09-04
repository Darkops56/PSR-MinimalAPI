namespace BearPizzeria.Mvc.Models.DTOs;

public record PizzaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    string Tamano,
    int Stock = 10
);

public record CarritoItemDto(
    int Id,
    int PizzaId,
    string PizzaNombre,
    string? PizzaDescripcion,
    string Tamano,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal
);

public record CarritoDto(
    int Id,
    int ClienteId,
    DateTime FechaActualizacion,
    decimal Total,
    int CantidadTotalItems,
    List<CarritoItemDto> Items
);

public record AgregarItemCarritoDto(
    int PizzaId,
    string Tamano,
    int Cantidad
);

public record ActualizarItemCarritoDto(
    int Cantidad
);

public record PedidoPizzaDto(
    int PizzaId,
    string PizzaNombre,
    string Tamano,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal
);

public record PedidoDto(
    int Id,
    int ClienteId,
    string ClienteUsuario,
    DateTime FechaPedido,
    string Estado,
    decimal Total,
    string DireccionEntrega,
    List<PedidoPizzaDto> Items
);

public record ClienteDto(
    int Id,
    string Usuario,
    string Nombre,
    string Direccion,
    string Telefono,
    string Email
);

public record AuthResponseDto(
    int ClienteId,
    string Username,
    string Nombre,
    string Email,
    string Direccion,
    string Telefono,
    string Token = ""
);

public record RegisterDto(
    string Nombre,
    string Direccion,
    string Telefono,
    string Email,
    string Username,
    string Password
);

public record LoginDto(
    string Username,
    string Password
);
