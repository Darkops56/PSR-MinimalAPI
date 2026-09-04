namespace BearPizzeria.Api.Models.DTOs;

public class PedidoResponse
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteUsuario { get; set; } = string.Empty;
    public DateTime FechaPedido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string DireccionEntrega { get; set; } = string.Empty;
    public List<PedidoPizzaResponse> Items { get; set; } = [];
}
