using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Api.Validators;
using Xunit;

namespace BearPizzeria.Api.Tests.Validators;

public class PedidosYItemsValidatorsTests
{
    private readonly CrearPedidoRequestValidator _crearPedidoValidator = new();
    private readonly PedidoItemRequestValidator _pedidoItemValidator = new();
    private readonly ActualizarEstadoRequestValidator _actualizarEstadoValidator = new();
    private readonly CrearPizzaRequestValidator _crearPizzaValidator = new();
    private readonly ActualizarClienteRequestValidator _actualizarClienteValidator = new();

    [Fact]
    public void CrearPedidoValidator_ConItemsValidos_DebeSerValido()
    {
        var request = new CrearPedidoRequest
        {
            ClienteUsuario = "juan_perez",
            Items = new List<PedidoItemRequest>
            {
                new() { PizzaNombre = "Muzzarella", Cantidad = 2 }
            }
        };

        var result = _crearPedidoValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CrearPedidoValidator_SinUsuarioOSinItems_DebeFallarConMensajeEntendible()
    {
        var request = new CrearPedidoRequest
        {
            ClienteUsuario = "",
            Items = new List<PedidoItemRequest>()
        };

        var result = _crearPedidoValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ClienteUsuario" && e.ErrorMessage.Contains("El usuario del cliente es obligatorio"));
        Assert.Contains(result.Errors, e => e.PropertyName == "Items" && e.ErrorMessage.Contains("El pedido debe contener al menos una pizza"));
    }

    [Theory]
    [InlineData("", 1, "El nombre de la pizza es obligatorio")]
    [InlineData("Muzzarella", 0, "La cantidad de pizzas debe ser al menos 1")]
    [InlineData("Muzzarella", -2, "La cantidad de pizzas debe ser al menos 1")]
    public void PedidoItemValidator_DatosInvalidos_DebeFallar(string pizza, int cantidad, string mensajeEsperado)
    {
        var item = new PedidoItemRequest
        {
            PizzaNombre = pizza,
            Cantidad = cantidad
        };
        var result = _pedidoItemValidator.Validate(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(mensajeEsperado));
    }

    [Theory]
    [InlineData("EnPreparacion", true)]
    [InlineData("EnViaje", true)]
    [InlineData("Entregado", true)]
    [InlineData("Cancelado", false)]
    [InlineData("Inexistente", false)]
    [InlineData("", false)]
    public void ActualizarEstadoValidator_Estados_VerificaCorrectamente(string estado, bool esperadoValido)
    {
        var request = new ActualizarEstadoRequest { Estado = estado };
        var result = _actualizarEstadoValidator.Validate(request);

        Assert.Equal(esperadoValido, result.IsValid);
        if (!esperadoValido)
        {
            Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Estado inválido") || e.ErrorMessage.Contains("El estado es obligatorio"));
        }
    }

    [Theory]
    [InlineData("Fugazzeta", 5000, "Grande", true)]
    [InlineData("", 5000, "Grande", false)]
    [InlineData("Fugazzeta", 0, "Grande", false)]
    [InlineData("Fugazzeta", -50, "Grande", false)]
    [InlineData("Fugazzeta", 150000, "Grande", false)]
    [InlineData("Fugazzeta", 5000, "GigantescaInvalida", false)]
    public void CrearPizzaValidator_ReglasDeNegocio_VerificaCorrectamente(string nombre, decimal precio, string tamano, bool esperadoValido)
    {
        var request = new CrearPizzaRequest
        {
            Nombre = nombre,
            Precio = precio,
            Tamano = tamano
        };
        var result = _crearPizzaValidator.Validate(request);

        Assert.Equal(esperadoValido, result.IsValid);
    }

    [Fact]
    public void ActualizarClienteValidator_DatosValidos_DebeSerValido()
    {
        var request = new ActualizarClienteRequest(
            "juan_modificado",
            "Juan Perez Modificado",
            "Calle Nueva 456",
            "+54 11 9999-8888",
            "juan.nuevo@correo.com"
        );

        var result = _actualizarClienteValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ActualizarClienteValidator_TelefonoOEmailInvalido_DebeFallarConMensajeEntendible()
    {
        var request = new ActualizarClienteRequest(
            "juan_modificado",
            "Juan Perez",
            "Calle 123",
            "telefono_no_numerico",
            "correo_invalido"
        );

        var result = _actualizarClienteValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Telefono" && e.ErrorMessage.Contains("El formato del teléfono no es válido"));
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.ErrorMessage.Contains("El formato del email no es válido"));
    }
}
