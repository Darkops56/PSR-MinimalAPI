namespace BearPizzeria.Api.Models.DTOs;

public record CrearDireccionRequest(
    string Nombre,
    string DireccionCompleta,
    string? Notas = null,
    bool EsPrincipal = false
);

public record ActualizarDireccionRequest(
    string Nombre,
    string DireccionCompleta,
    string? Notas = null,
    bool EsPrincipal = false
);

public record DireccionResponse(
    int Id,
    int ClienteId,
    string Nombre,
    string DireccionCompleta,
    string? Notas,
    bool EsPrincipal
);

public record PizzaSinStockItem(
    int PizzaId,
    string PizzaNombre,
    string Tamano,
    int CantidadSolicitada,
    int CantidadDisponible,
    int CantidadAgregada
);

public record RepetirPedidoResponse(
    bool TotalmenteAgregado,
    string Mensaje,
    List<PizzaSinStockItem> ItemsSinStockSuficiente,
    CarritoResponse CarritoActualizado
);

public record CheckoutRequest(
    int? DireccionId = null,
    string? DireccionEntrega = null
);
