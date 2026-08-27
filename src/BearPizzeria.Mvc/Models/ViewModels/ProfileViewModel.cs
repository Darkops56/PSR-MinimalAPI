using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Models.ViewModels;

public class ProfileViewModel
{
    public AuthResponseDto? User { get; set; }
    public List<PedidoDto> Pedidos { get; set; } = [];
    public PedidoDto? ActiveOrder => Pedidos.FirstOrDefault(p => p.Estado == "EnPreparacion" || p.Estado == "EnViaje");
    public int? ActiveOrderId { get; set; }
}
