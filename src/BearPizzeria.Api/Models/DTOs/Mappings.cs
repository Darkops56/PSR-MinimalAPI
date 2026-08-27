namespace BearPizzeria.Api.Models.DTOs;

public static class Mappings
{
    public static ClienteResponse ToResponse(this Cliente cliente)
        => new()
        {
            Id = cliente.Id,
            Usuario = cliente.Usuario?.Username ?? "",
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
            ClienteUsuario = pedido.Cliente?.Usuario?.Username ?? "",
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
            Tamano = pp.Tamano.ToString(),
            Cantidad = pp.Cantidad,
            PrecioUnitario = pp.PrecioUnitario,
            Subtotal = pp.Subtotal
        };

    public static CarritoItemResponse ToResponse(this CarritoItem item)
        => new(
            item.Id,
            item.PizzaId,
            item.Pizza?.Nombre ?? "",
            item.Pizza?.Descripcion,
            item.Tamano.ToString(),
            item.Cantidad,
            item.PrecioUnitario,
            item.Subtotal
        );

    public static CarritoResponse ToResponse(this Carrito carrito)
    {
        var items = carrito.Items?.Select(i => i.ToResponse()).ToList() ?? [];
        return new(
            carrito.Id,
            carrito.ClienteId,
            carrito.FechaActualizacion,
            carrito.Total,
            items.Sum(i => i.Cantidad),
            items
        );
    }
}
