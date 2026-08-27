namespace BearPizzeria.Api.Models;

public class PedidoPizza
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public int PizzaId { get; set; }
    public TamanoPizza Tamano { get; set; } = TamanoPizza.Grande;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public Pedido Pedido { get; set; } = null!;
    public Pizza Pizza { get; set; } = null!;
}
