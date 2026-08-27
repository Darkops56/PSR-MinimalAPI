namespace BearPizzeria.Api.Models;

public class Carrito
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public ICollection<CarritoItem> Items { get; set; } = new List<CarritoItem>();
}
