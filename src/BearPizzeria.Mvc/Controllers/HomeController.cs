using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IPizzaApiService _pizzaApiService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IPizzaApiService pizzaApiService, ILogger<HomeController> logger)
    {
        _pizzaApiService = pizzaApiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var pizzas = await _pizzaApiService.GetPizzasAsync();

        var viewModel = new HomeViewModel
        {
            Pizzas = pizzas,
            FeaturedPizzas = pizzas.Take(3).ToList(),
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        };

        if (viewModel.IsAuthenticated)
        {
            var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(clienteIdClaim, out var clienteId))
            {
                viewModel.CurrentUser = new AuthResponseDto(
                    clienteId,
                    User.Identity?.Name ?? "",
                    User.FindFirst("Nombre")?.Value ?? "",
                    User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                    User.FindFirst("Direccion")?.Value ?? "",
                    User.FindFirst("Telefono")?.Value ?? ""
                );
            }
        }

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
