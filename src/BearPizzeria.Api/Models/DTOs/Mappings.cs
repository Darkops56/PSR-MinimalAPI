namespace BearPizzeria.Api.Models.DTOs;

public static class Mappings
{
    public static ClienteResponse ToResponse(this Cliente cliente)
        => new()
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Direccion = cliente.Direccion,
            Telefono = cliente.Telefono,
            Email = cliente.Email
        };

    public static PizzaResponse ToResponse(this Pizza pizza)
        => new()
        {
            Id = pizza.Id,
            Nombre = pizza.Nombre,
            Descripcion = pizza.Descripcion,
            Precio = pizza.Precio,
            Tamano = pizza.Tamano.ToString()
        };

    public static PedidoResponse ToResponse(this Pedido pedido)
        => new()
        {
            Id = pedido.Id,
            ClienteId = pedido.ClienteId,
            ClienteNombre = pedido.Cliente?.Nombre ?? "",
            FechaPedido = pedido.FechaPedido,
            Estado = pedido.Estado.ToString(),
            Total = pedido.Total,
            Items = pedido.PedidoPizzas?.Select(pp => pp.ToResponse()).ToList() ?? []
        };

    public static PedidoPizzaResponse ToResponse(this PedidoPizza pp)
        => new()
        {
            PizzaId = pp.PizzaId,
            PizzaNombre = pp.Pizza?.Nombre ?? "",
            Cantidad = pp.Cantidad,
            PrecioUnitario = pp.PrecioUnitario
        };
}
