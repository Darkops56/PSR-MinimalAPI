namespace BearPizzeria.Api.Models.DTOs;

public class PedidoItemRequest
{
    public string PizzaNombre { get; set; } = string.Empty;
    public string Tamano { get; set; } = "Grande";
    public int Cantidad { get; set; } = 1;
}
