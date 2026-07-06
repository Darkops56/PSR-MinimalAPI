namespace BearPizzeria.Api.Models.DTOs;

public class PedidoItemRequest
{
    public int PizzaId { get; set; }
    public int Cantidad { get; set; } = 1;
}
