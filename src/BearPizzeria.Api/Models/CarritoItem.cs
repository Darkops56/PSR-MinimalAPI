namespace BearPizzeria.Api.Models;

public class CarritoItem
{
    public int Id { get; set; }
    public int CarritoId { get; set; }
    public int PizzaId { get; set; }
    public TamanoPizza Tamano { get; set; } = TamanoPizza.Grande;
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public Carrito Carrito { get; set; } = null!;
    public Pizza Pizza { get; set; } = null!;
}
