using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Models.ViewModels;

public class CartViewModel
{
    public CarritoDto? Carrito { get; set; }
    public bool IsAuthenticated { get; set; }
    public AuthResponseDto? CurrentUser { get; set; }
    public decimal CostoEnvio { get; set; } = 0.00m; // Envío gratis
    public decimal TotalConEnvio => (Carrito?.Total ?? 0.00m) + CostoEnvio;
}
