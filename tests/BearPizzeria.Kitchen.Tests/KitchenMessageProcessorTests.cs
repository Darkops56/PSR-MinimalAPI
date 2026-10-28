using System.Text.Json;
using BearPizzeria.Kitchen;
using Xunit;

namespace BearPizzeria.Kitchen.Tests;

public class KitchenMessageProcessorTests
{
    [Fact]
    public void CreateHandshake_CU12_GeneraHandshakeValidoConTipoCocina()
    {
        var handshake = KitchenMessageProcessor.CreateHandshake();

        Assert.NotNull(handshake);
        Assert.EndsWith("\n", handshake);

        using var doc = JsonDocument.Parse(handshake);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("Tipo", out var tipo));
        Assert.Equal("COCINA", tipo.GetString());
    }

    [Fact]
    public async Task ProcesarMensaje_CU12_CU13_ConNuevoPedidoValido_GeneraActualizarEstadoEnViaje()
    {
        var jsonNuevoPedido = JsonSerializer.Serialize(new
        {
            TipoMensaje = "NuevoPedido",
            Id = 42,
            Detalle = new
            {
                Cliente = "Juan Perez",
                Direccion = "Av. Corrientes 1234",
                Items = new[]
                {
                    new { Pizza = "Muzzarella", Cantidad = 2, PrecioUnitario = 4500.00m },
                    new { Pizza = "Calabresa", Cantidad = 1, PrecioUnitario = 5200.00m }
                },
                Total = 14200.00m
            }
        });

        bool coccionSimulada = false;
        var respuesta = await KitchenMessageProcessor.ProcesarMensajeAsync(
            jsonNuevoPedido,
            simularCoccion: () =>
            {
                coccionSimulada = true;
                return Task.CompletedTask;
            }
        );

        Assert.True(coccionSimulada);
        Assert.NotNull(respuesta);
        Assert.EndsWith("\n", respuesta);

        using var doc = JsonDocument.Parse(respuesta);
        var root = doc.RootElement;

        Assert.Equal("ActualizarEstado", root.GetProperty("TipoMensaje").GetString());
        Assert.Equal(42, root.GetProperty("PedidoId").GetInt32());
        Assert.Equal("EnViaje", root.GetProperty("Estado").GetString());
    }

    [Theory]
    [InlineData("{\"TipoMensaje\":\"NuevoPedido\", \"Id\": 10 ")] // JSON trunco
    [InlineData("esto no es un json")]
    [InlineData("{")]
    public async Task ProcesarMensaje_CU12_JSONCorrupto_NoLanzaExcepcionYRetornaNull(string jsonInvalido)
    {
        var respuesta = await KitchenMessageProcessor.ProcesarMensajeAsync(
            jsonInvalido,
            simularCoccion: () => Task.CompletedTask
        );

        Assert.Null(respuesta);
    }

    [Theory]
    [InlineData("{\"TipoMensaje\":\"MensajeDesconocido\"}")]
    [InlineData("{\"Tipo\":\"REPARTO\"}")]
    [InlineData("{}")]
    public async Task ProcesarMensaje_CU12_MensajesNoRelevantes_RetornaNull(string jsonOtroTipo)
    {
        var respuesta = await KitchenMessageProcessor.ProcesarMensajeAsync(
            jsonOtroTipo,
            simularCoccion: () => Task.CompletedTask
        );

        Assert.Null(respuesta);
    }
}

