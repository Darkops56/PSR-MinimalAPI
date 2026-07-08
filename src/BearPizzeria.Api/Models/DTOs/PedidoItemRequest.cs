namespace BearPizzeria.Api.Models.DTOs;

public class PedidoItemRequest
{
    public string PizzaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; } = 1;
}
