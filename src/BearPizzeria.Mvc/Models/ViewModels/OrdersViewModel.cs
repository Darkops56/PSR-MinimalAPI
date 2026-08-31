using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Models.ViewModels;

public class OrdersViewModel
{
    public List<PedidoDto> Pedidos { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public string? FechaFiltro { get; set; }
    public AuthResponseDto? User { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
