namespace BearPizzeria.Api.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;

    public ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
}
