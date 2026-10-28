using System.Net;
using System.Net.Http.Json;
using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Common.Testing;
using Xunit;

namespace BearPizzeria.Api.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_CU02_ConDatosValidos_CreaCuentaYRetorna201ConToken()
    {
        var username = $"usuario_{Guid.NewGuid():N}"[..12];
        var request = new RegisterRequest(
            Nombre: "Cliente Test",
            Telefono: "+54 11 5555-4321",
            Email: $"{username}@correo.com",
            Username: username,
            Password: "password123",
            Direccion: "Av. Siempre Viva 742"
        );

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.Equal(username, auth.Username);
        Assert.NotEmpty(auth.Token);
        Assert.True(auth.ClienteId > 0);
    }

    [Fact]
    public async Task Register_CU02_ConUsernameDuplicado_DebeFallarConMensajeEntendible()
    {
        var username = $"duplicado_{Guid.NewGuid():N}"[..12];
        var request = new RegisterRequest(
            Nombre: "Cliente Original",
            Telefono: "+54 11 1111-2222",
            Email: $"{username}@correo.com",
            Username: username,
            Password: "password123",
            Direccion: "Calle 1"
        );

        var primerResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, primerResponse.StatusCode);

        var requestDuplicado = new RegisterRequest(
            Nombre: "Cliente Duplicado",
            Telefono: "+54 11 3333-4444",
            Email: $"otro_{username}@correo.com",
            Username: username,
            Password: "password123",
            Direccion: "Calle 2"
        );

        var segundoResponse = await _client.PostAsJsonAsync("/api/auth/register", requestDuplicado);

        Assert.Equal(HttpStatusCode.BadRequest, segundoResponse.StatusCode);
        var error = await segundoResponse.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("AUTH-400", error.Codigo);
        Assert.Contains(error.Errores!, e => e.Campo == "Username" && e.Mensaje.Contains("ya está registrado"));
    }

    [Fact]
    public async Task Register_CU02_ConEmailDuplicado_DebeFallarConMensajeEntendible()
    {
        var baseId = Guid.NewGuid().ToString("N")[..8];
        var email = $"email_duplicado_{baseId}@correo.com";

        var req1 = new RegisterRequest(Nombre: "User 1", Telefono: "+54 11 1111-2222", Email: email, Username: $"u1_{baseId}", Password: "password123", Direccion: "Calle 1");
        var res1 = await _client.PostAsJsonAsync("/api/auth/register", req1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        var req2 = new RegisterRequest(Nombre: "User 2", Telefono: "+54 11 3333-4444", Email: email, Username: $"u2_{baseId}", Password: "password123", Direccion: "Calle 2");
        var res2 = await _client.PostAsJsonAsync("/api/auth/register", req2);

        Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);
        var error = await res2.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("AUTH-400", error.Codigo);
        Assert.Contains(error.Errores!, e => e.Campo == "Email" && e.Mensaje.Contains("ya está registrado"));
    }

    [Fact]
    public async Task Login_CU03_ConCredencialesValidas_Retorna200YToken()
    {
        var username = $"login_{Guid.NewGuid():N}"[..12];
        const string password = "PasswordSeguro123";

        var reg = new RegisterRequest(Nombre: "Usuario Login", Telefono: "+54 11 8888-9999", Email: $"{username}@correo.com", Username: username, Password: password, Direccion: "Calle 3");
        await _client.PostAsJsonAsync("/api/auth/register", reg);

        var loginReq = new LoginRequest(username, password);
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.Equal(username, auth.Username);
        Assert.NotEmpty(auth.Token);
    }

    [Fact]
    public async Task Login_CU03_ConPasswordIncorrecto_DebeFallarConMensajeEntendible()
    {
        var username = $"login_fail_{Guid.NewGuid():N}"[..12];
        var reg = new RegisterRequest(Nombre: "Usuario", Telefono: "+54 11 8888-9999", Email: $"{username}@correo.com", Username: username, Password: "PasswordCorrecto123", Direccion: "Calle 3");
        await _client.PostAsJsonAsync("/api/auth/register", reg);

        var loginReq = new LoginRequest(username, "PasswordIncorrecto");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("AUTH-401", error.Codigo);
        Assert.Equal("Usuario o contraseña incorrectos.", error.Mensaje);
    }

    [Fact]
    public async Task Login_CU03_ConUsuarioInexistente_DebeFallarConMismoMensajePorSeguridad()
    {
        var loginReq = new LoginRequest("usuario_fantasma_xyz", "Cualquiera123");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.NotNull(error);
        Assert.Equal("AUTH-401", error.Codigo);
        Assert.Equal("Usuario o contraseña incorrectos.", error.Mensaje);
    }
}
