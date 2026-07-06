namespace BearPizzeria.Api.Models;

public class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
    public EstadoPedido Estado { get; set; } = EstadoPedido.EsperaDeConfirmacion;
    public decimal Total { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public ICollection<PedidoPizza> PedidoPizzas { get; set; } = new List<PedidoPizza>();
}
