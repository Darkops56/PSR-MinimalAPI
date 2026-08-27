using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IPedidoApiService _pedidoApiService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IPedidoApiService pedidoApiService, ILogger<ProfileController> logger)
    {
        _pedidoApiService = pedidoApiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clienteIdClaim, out var clienteId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var token = User.FindFirst("Token")?.Value;
        var pedidos = await _pedidoApiService.GetPedidosByClienteAsync(clienteId, token);

        var viewModel = new ProfileViewModel
        {
            User = new AuthResponseDto(
                clienteId,
                User.Identity?.Name ?? "",
                User.FindFirst("Nombre")?.Value ?? "",
                User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                User.FindFirst("Direccion")?.Value ?? "",
                User.FindFirst("Telefono")?.Value ?? "",
                token ?? ""
            ),
            Pedidos = pedidos,
            ActiveOrderId = pedidos.FirstOrDefault(p => p.Estado == "EnPreparacion" || p.Estado == "EnViaje")?.Id
        };

        return View(viewModel);
    }
}
