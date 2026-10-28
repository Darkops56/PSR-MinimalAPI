using BearPizzeria.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BearPizzeria.Api.Tests.Services;

public class SecurityServicesTests
{
    private readonly PasswordHasherService _hasher = new();
    private readonly TokenService _tokenService;

    public SecurityServicesTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave_secreta_super_segura_de_al_menos_32_caracteres_12345"
            })
            .Build();

        _tokenService = new TokenService(config);
    }

    [Fact]
    public void PasswordHasher_HashPassword_GeneraHashNoVacioDistintoDelTextoPlano()
    {
        const string rawPass = "MiPasswordSecreto123!";
        var hash = _hasher.HashPassword(rawPass);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(rawPass, hash);
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_ConPasswordCorrecto_RetornaTrue()
    {
        const string rawPass = "MiPasswordSecreto123!";
        var hash = _hasher.HashPassword(rawPass);

        var resultado = _hasher.VerifyPassword(rawPass, hash);

        Assert.True(resultado);
    }

    [Fact]
    public void PasswordHasher_VerifyPassword_ConPasswordIncorrecto_RetornaFalse()
    {
        const string rawPass = "MiPasswordSecreto123!";
        var hash = _hasher.HashPassword(rawPass);

        var resultado = _hasher.VerifyPassword("ContraseñaEquivocada", hash);

        Assert.False(resultado);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("pass", "")]
    [InlineData("pass", "hash_invalido_sin_formato")]
    public void PasswordHasher_VerifyPassword_EntradasInvalidas_NoLanzaExcepcionYRetornaFalse(string raw, string hash)
    {
        var resultado = _hasher.VerifyPassword(raw, hash);
        Assert.False(resultado);
    }

    [Fact]
    public void TokenService_GenerarYValidarToken_ValidaCorrectamenteClienteYUsuario()
    {
        const int clienteId = 42;
        const string username = "bear_lover";
        const string email = "bear@pizzeria.com";

        var token = _tokenService.GenerarToken(clienteId, username, email);
        Assert.NotNull(token);

        var (esValido, resClienteId, resUsername) = _tokenService.ValidarToken("Bearer " + token);

        Assert.True(esValido);
        Assert.Equal(clienteId, resClienteId);
        Assert.Equal(username, resUsername);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("TokenSinBearer")]
    [InlineData("Bearer token.invalido.corrupto")]
    public void TokenService_ValidarToken_EntradasInvalidas_RetornaInvalido(string? authHeader)
    {
        var (esValido, clienteId, username) = _tokenService.ValidarToken(authHeader);

        Assert.False(esValido);
        Assert.Null(clienteId);
        Assert.Null(username);
    }
}
