using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace BearPizzeria.Mvc.Tests.Fakes;

public static class ControllerTestHelper
{
    public static void SetupTestContext(
        this Controller controller,
        ClaimsPrincipal? user = null,
        ITempDataDictionary? tempData = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRouting();
        services.AddMvcCore();
        services.AddAuthentication();
        services.AddSingleton<IAuthenticationService, FakeAuthenticationService>();

        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = sp,
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
        };

        var actionContext = new ActionContext
        {
            HttpContext = httpContext,
            RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
            ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
        };

        controller.ControllerContext = new ControllerContext(actionContext);
        controller.TempData = tempData ?? new TempDataDictionary(httpContext, new FakeTempDataProvider());
        controller.Url = new FakeUrlHelper(actionContext);
    }

    public static ClaimsPrincipal CreateUserPrincipal(int clienteId, string username = "usuario_test", string token = "token_jwt_test")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, clienteId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Email, $"{username}@correo.com"),
            new("Nombre", "Nombre Real"),
            new("Direccion", "Calle Falsa 123"),
            new("Telefono", "+54 11 1234-5678"),
            new("Token", token)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}

public class FakeUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; }

    public FakeUrlHelper(ActionContext actionContext)
    {
        ActionContext = actionContext;
    }

    public string? Action(UrlActionContext actionContext) => $"/{actionContext.Controller}/{actionContext.Action}";
    public string? Content(string? contentPath) => contentPath;
    public bool IsLocalUrl(string? url) => !string.IsNullOrEmpty(url) && url.StartsWith("/");
    public string? RouteUrl(UrlRouteContext routeContext) => "/route";
    public string? Link(string? routeName, object? values) => "/link";
}

public class FakeAuthenticationService : IAuthenticationService
{
    public bool SignInCalled { get; private set; }
    public bool SignOutCalled { get; private set; }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult.NoResult());

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        SignInCalled = true;
        context.User = principal;
        return Task.CompletedTask;
    }

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        SignOutCalled = true;
        context.User = new ClaimsPrincipal(new ClaimsIdentity());
        return Task.CompletedTask;
    }
}

public class FakeTempDataProvider : ITempDataProvider
{
    private readonly Dictionary<string, object> _data = new();

    public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        foreach (var kvp in values)
        {
            _data[kvp.Key] = kvp.Value;
        }
    }
}
