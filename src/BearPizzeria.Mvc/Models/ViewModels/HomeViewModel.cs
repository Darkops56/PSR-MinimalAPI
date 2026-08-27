using BearPizzeria.Mvc.Models.DTOs;

namespace BearPizzeria.Mvc.Models.ViewModels;

public class HomeViewModel
{
    public List<PizzaDto> Pizzas { get; set; } = [];
    public List<PizzaDto> FeaturedPizzas { get; set; } = [];
    public List<string> Categorias { get; set; } = ["Todas", "Clásicas", "Especiales", "Gourmet"];
    public string CategoriaSeleccionada { get; set; } = "Todas";
    public bool IsAuthenticated { get; set; }
    public AuthResponseDto? CurrentUser { get; set; }
}
