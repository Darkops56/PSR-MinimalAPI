namespace BearPizzeria.Api.Models.DTOs;

public class CrearPedidoRequest
{
    public int ClienteId { get; set; }
    public List<PedidoItemRequest> Items { get; set; } = [];
}
