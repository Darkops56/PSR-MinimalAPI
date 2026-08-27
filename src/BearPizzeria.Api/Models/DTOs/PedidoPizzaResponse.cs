namespace BearPizzeria.Api.Models.DTOs;

public class PedidoPizzaResponse
{
    public int PizzaId { get; set; }
    public string PizzaNombre { get; set; } = string.Empty;
    public string Tamano { get; set; } = "Grande";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
