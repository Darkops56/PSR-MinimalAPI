namespace BearPizzeria.Api.Models.DTOs;

public class CrearPedidoRequest
{
    public string ClienteNombre { get; set; } = string.Empty;
    public List<PedidoItemRequest> Items { get; set; } = [];
}
