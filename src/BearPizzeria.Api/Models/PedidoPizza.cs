namespace BearPizzeria.Api.Models;

public class PedidoPizza
{
    public int PedidoId { get; set; }
    public int PizzaId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Pizza Pizza { get; set; } = null!;
}
