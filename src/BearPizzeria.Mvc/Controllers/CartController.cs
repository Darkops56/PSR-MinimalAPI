using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Controllers;

public class CartController : Controller
{
    private readonly ICarritoApiService _carritoApiService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICarritoApiService carritoApiService, ILogger<CartController> logger)
    {
        _carritoApiService = carritoApiService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var isAuth = User.Identity?.IsAuthenticated ?? false;
        var viewModel = new CartViewModel
        {
            IsAuthenticated = isAuth
        };

        if (isAuth)
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

                viewModel.Carrito = await _carritoApiService.GetCarritoAsync(clienteId);
            }
        }

        return View(viewModel);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clienteIdClaim, out var clienteId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var pedido = await _carritoApiService.CheckoutCarritoAsync(clienteId);
        if (pedido is not null)
        {
            TempData["SuccessMessage"] = $"¡Pedido #{pedido.Id} confirmado con éxito!";
            return RedirectToAction("Index", "Profile");
        }

        TempData["ErrorMessage"] = "No se pudo procesar el pedido. Asegurate de tener pizzas en el carrito.";
        return RedirectToAction("Index");
    }
}
