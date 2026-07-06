namespace BearPizzeria.Api.Models.DTOs;

public class PedidoResponse
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public DateTime FechaPedido { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<PedidoPizzaResponse> Items { get; set; } = [];
}
