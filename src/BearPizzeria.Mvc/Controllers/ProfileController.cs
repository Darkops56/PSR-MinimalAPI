using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
    private readonly IUsuarioApiService _usuarioApiService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IPedidoApiService pedidoApiService,
        IUsuarioApiService usuarioApiService,
        ILogger<ProfileController> logger)
    {
        _pedidoApiService = pedidoApiService;
        _usuarioApiService = usuarioApiService;
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
        var pedidosTask = _pedidoApiService.GetPedidosByClienteAsync(clienteId, token);
        var activeOrdersTask = _pedidoApiService.GetMisPedidosActivosAsync(token);

        await Task.WhenAll(pedidosTask, activeOrdersTask);

        var pedidos = await pedidosTask;
        var activeOrders = await activeOrdersTask;

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
            ActiveOrders = activeOrders,
            ActiveOrderId = activeOrders.FirstOrDefault()?.Id ?? pedidos.FirstOrDefault(p => p.Estado == "EnPreparacion" || p.Estado == "EnViaje")?.Id
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Trackers()
    {
        var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clienteIdClaim, out var clienteId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var token = User.FindFirst("Token")?.Value;
        var activeOrders = await _pedidoApiService.GetMisPedidosActivosAsync(token);

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
            ActiveOrders = activeOrders,
            ActiveOrderId = activeOrders.FirstOrDefault()?.Id
        };

        return View("Trackers", viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Orders(int page = 1, string? fecha = null)
    {
        var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clienteIdClaim, out var clienteId))
        {
            return RedirectToAction("Login", "Auth");
        }

        var token = User.FindFirst("Token")?.Value;
        var allPedidos = await _pedidoApiService.GetPedidosByClienteAsync(clienteId, token);

        List<PedidoDto> baseList;
        DateTime filterDate = default;
        bool hasFilter = !string.IsNullOrWhiteSpace(fecha) && DateTime.TryParse(fecha, out filterDate);

        if (hasFilter)
        {
            baseList = allPedidos
                .Where(p => p.FechaPedido.ToLocalTime().Date == filterDate.Date)
                .ToList();
        }
        else
        {
            // Omitir los primeros 3 que ya se muestran en el perfil principal
            baseList = allPedidos.Skip(3).ToList();
        }

        int pageSize = 10;
        int currentPage = Math.Max(1, page);
        int totalCount = baseList.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

        if (currentPage > totalPages) currentPage = totalPages;

        var pagedItems = baseList
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var ordersVm = new OrdersViewModel
        {
            Pedidos = pagedItems,
            PageNumber = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount,
            FechaFiltro = hasFilter ? filterDate.ToString("yyyy-MM-dd") : null,
            User = new AuthResponseDto(
                clienteId,
                User.Identity?.Name ?? "",
                User.FindFirst("Nombre")?.Value ?? "",
                User.FindFirst(ClaimTypes.Email)?.Value ?? "",
                User.FindFirst("Direccion")?.Value ?? "",
                User.FindFirst("Telefono")?.Value ?? "",
                token ?? ""
            )
        };

        return View("Orders", ordersVm);
    }

    [HttpGet("/Profile/ActiveOrdersJson")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ActiveOrdersJson()
    {
        var token = User.FindFirst("Token")?.Value;
        var activeOrders = await _pedidoApiService.GetMisPedidosActivosAsync(token);
        return Json(activeOrders);
    }

    [HttpGet("/Profile/ActiveOrderJson")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ActiveOrderJson()
    {
        var token = User.FindFirst("Token")?.Value;
        var activeOrder = await _pedidoApiService.GetMiPedidoActivoAsync(token);
        return Json(activeOrder);
    }

    [HttpGet("/Profile/AllOrdersJson")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> AllOrdersJson()
    {
        var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clienteIdClaim, out var clienteId))
        {
            return Unauthorized();
        }

        var token = User.FindFirst("Token")?.Value;
        var allOrders = await _pedidoApiService.GetPedidosByClienteAsync(clienteId, token);
        return Json(allOrders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(EditProfileViewModel model)
    {
        var clienteIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(clienteIdClaim, out var clienteId))
        {
            return RedirectToAction("Login", "Auth");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Por favor corregí los errores en el formulario de perfil.";
            return RedirectToAction("Index");
        }

        var token = User.FindFirst("Token")?.Value;
        var request = new ActualizarClienteDto(model.Username, model.Nombre, model.Direccion ?? "", model.Telefono, model.Email);

        var clienteUpdated = await _usuarioApiService.UpdateClienteAsync(clienteId, request, token);
        if (clienteUpdated is null)
        {
            TempData["ErrorMessage"] = "No se pudieron actualizar los datos. Verificá si el usuario o email ya pertenecen a otra cuenta.";
            return RedirectToAction("Index");
        }

        await RefreshUserClaimsAsync(clienteUpdated, token ?? "");

        TempData["SuccessMessage"] = "¡Perfil y usuario actualizados con éxito!";
        return RedirectToAction("Index");
    }

    private async Task RefreshUserClaimsAsync(ClienteDto clienteUpdated, string token)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, clienteUpdated.Id.ToString()),
            new(ClaimTypes.Name, clienteUpdated.Usuario),
            new(ClaimTypes.Email, clienteUpdated.Email),
            new("Nombre", clienteUpdated.Nombre),
            new("Direccion", clienteUpdated.Direccion),
            new("Telefono", clienteUpdated.Telefono),
            new("Token", token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            });
    }
}
