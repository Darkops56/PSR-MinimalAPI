namespace BearPizzeria.Api.Models.DTOs;

public record AgregarCarritoItemRequest(
    int PizzaId,
    string Tamano,
    int Cantidad
);

public record ActualizarCarritoItemRequest(
    int Cantidad
);

public record CarritoItemResponse(
    int Id,
    int PizzaId,
    string PizzaNombre,
    string? PizzaDescripcion,
    string Tamano,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal
);

public record CarritoResponse(
    int Id,
    int ClienteId,
    DateTime FechaActualizacion,
    decimal Total,
    int CantidadTotalItems,
    List<CarritoItemResponse> Items
);
