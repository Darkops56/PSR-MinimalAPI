using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using BearPizzeria.Mvc.Models.ViewModels;
using BearPizzeria.Mvc.Models.DTOs;
using BearPizzeria.Mvc.Services.Interfaces;

namespace BearPizzeria.Mvc.Controllers;

public class AuthController : Controller
{
    private readonly IUsuarioApiService _usuarioApiService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsuarioApiService usuarioApiService, ILogger<AuthController> logger)
    {
        _usuarioApiService = usuarioApiService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var authResult = await _usuarioApiService.LoginAsync(new LoginDto(model.Username, model.Password));
        if (authResult is null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        await SignInUserAsync(authResult);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated ?? false)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var registerDto = new RegisterDto(
            model.Nombre,
            model.Telefono,
            model.Email,
            model.Username,
            model.Password,
            model.Direccion ?? ""
        );

        var authResult = await _usuarioApiService.RegisterAsync(registerDto);
        if (authResult is null)
        {
            ModelState.AddModelError(string.Empty, "No se pudo completar el registro. El usuario o email pueden ya estar en uso.");
            return View(model);
        }

        await SignInUserAsync(authResult);
        TempData["SuccessMessage"] = $"¡Bienvenido {authResult.Nombre}! Tu cuenta ha sido creada.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInUserAsync(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.ClienteId.ToString()),
            new(ClaimTypes.Name, auth.Username),
            new(ClaimTypes.Email, auth.Email),
            new("Nombre", auth.Nombre),
            new("Direccion", auth.Direccion),
            new("Telefono", auth.Telefono),
            new("Token", auth.Token)
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
