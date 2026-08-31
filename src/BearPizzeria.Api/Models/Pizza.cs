namespace BearPizzeria.Api.Models;

public class Pizza
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public TamanoPizza Tamano { get; set; }
    public int Stock { get; set; } = 10;

    public ICollection<PedidoPizza> PedidoPizzas { get; set; } = new List<PedidoPizza>();
}
