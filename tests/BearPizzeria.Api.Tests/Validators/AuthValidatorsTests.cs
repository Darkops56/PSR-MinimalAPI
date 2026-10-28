using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Api.Validators;
using Xunit;

namespace BearPizzeria.Api.Tests.Validators;

public class AuthValidatorsTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void RegisterValidator_ConDatosValidos_DebeSerValido()
    {
        var request = new RegisterRequest(
            Nombre: "Juan Perez",
            Telefono: "+54 11 5555-1234",
            Email: "juan@correo.com",
            Username: "juan_perez",
            Password: "password123",
            Direccion: "Av. Corrientes 1234"
        );

        var result = _registerValidator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "La contraseña es obligatoria.")]
    [InlineData("12345", "La contraseña debe tener al menos 6 caracteres.")]
    public void RegisterValidator_ContrasenaInvalida_DebeFallarConMensajeEntendible(string password, string mensajeEsperado)
    {
        var request = new RegisterRequest(
            Nombre: "Juan Perez",
            Telefono: "+54 11 5555-1234",
            Email: "juan@correo.com",
            Username: "juan_perez",
            Password: password,
            Direccion: "Av. Corrientes 1234"
        );

        var result = _registerValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Password" && e.ErrorMessage.Contains(mensajeEsperado));
    }

    [Theory]
    [InlineData("", "El usuario es obligatorio. Escribí un nombre de usuario.")]
    [InlineData("ab", "El usuario debe tener al menos 3 caracteres.")]
    [InlineData("usuario con espacios", "El usuario solo puede contener letras, números y guiones bajos.")]
    [InlineData("usuario@invalido!", "El usuario solo puede contener letras, números y guiones bajos.")]
    public void RegisterValidator_UsernameInvalido_DebeFallarConMensajeEntendible(string username, string mensajeEsperado)
    {
        var request = new RegisterRequest(
            Nombre: "Juan Perez",
            Telefono: "+54 11 5555-1234",
            Email: "juan@correo.com",
            Username: username,
            Password: "password123",
            Direccion: "Av. Corrientes 1234"
        );

        var result = _registerValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Username" && e.ErrorMessage.Contains(mensajeEsperado));
    }

    [Theory]
    [InlineData("", "El email es obligatorio. Escribí tu correo electrónico.")]
    [InlineData("correosinformatocorrecto", "El formato del email no es válido. Ej: usuario@correo.com")]
    [InlineData("correo@", "El formato del email no es válido. Ej: usuario@correo.com")]
    public void RegisterValidator_EmailInvalido_DebeFallarConMensajeEntendible(string email, string mensajeEsperado)
    {
        var request = new RegisterRequest(
            Nombre: "Juan Perez",
            Telefono: "+54 11 5555-1234",
            Email: email,
            Username: "juan_perez",
            Password: "password123",
            Direccion: "Av. Corrientes 1234"
        );

        var result = _registerValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.ErrorMessage.Contains(mensajeEsperado));
    }

    [Theory]
    [InlineData("", "El teléfono es obligatorio. Escribí un número de contacto.")]
    [InlineData("telefono-con-letras", "El formato del teléfono no es válido. Ej: +54 11 5555-1234")]
    public void RegisterValidator_TelefonoInvalido_DebeFallarConMensajeEntendible(string telefono, string mensajeEsperado)
    {
        var request = new RegisterRequest(
            Nombre: "Juan Perez",
            Telefono: telefono,
            Email: "juan@correo.com",
            Username: "juan_perez",
            Password: "password123",
            Direccion: "Av. Corrientes 1234"
        );

        var result = _registerValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Telefono" && e.ErrorMessage.Contains(mensajeEsperado));
    }

    [Fact]
    public void LoginValidator_ConDatosValidos_DebeSerValido()
    {
        var request = new LoginRequest("juan_perez", "password123");
        var result = _loginValidator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "password123", "El nombre de usuario es obligatorio")]
    [InlineData("juan_perez", "", "La contraseña es obligatoria")]
    public void LoginValidator_CamposVacios_DebeFallar(string username, string password, string mensajeEsperado)
    {
        var request = new LoginRequest(username, password);
        var result = _loginValidator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(mensajeEsperado));
    }
}
