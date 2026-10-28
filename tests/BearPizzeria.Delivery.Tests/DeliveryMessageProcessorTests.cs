using System.Text.Json;
using BearPizzeria.Delivery;
using Xunit;

namespace BearPizzeria.Delivery.Tests;

public class DeliveryMessageProcessorTests
{
    [Fact]
    public void CreateHandshake_CU15_GeneraHandshakeValidoConTipoReparto()
    {
        var handshake = DeliveryMessageProcessor.CreateHandshake();

        Assert.NotNull(handshake);
        Assert.EndsWith("\n", handshake);

        using var doc = JsonDocument.Parse(handshake);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("Tipo", out var tipo));
        Assert.Equal("REPARTO", tipo.GetString());
    }

    [Fact]
    public async Task ProcesarMensaje_CU15_CU16_ConPedidoEnViaje_GeneraActualizarEstadoEntregado()
    {
        var jsonPedidoEnViaje = JsonSerializer.Serialize(new
        {
            TipoMensaje = "PedidoEnViaje",
            Id = 88,
            Detalle = new
            {
                Cliente = "Maria Gomez",
                Direccion = "Calle Falsa 123"
            }
        });

        bool viajeSimulado = false;
        var respuesta = await DeliveryMessageProcessor.ProcesarMensajeAsync(
            jsonPedidoEnViaje,
            simularViaje: () =>
            {
                viajeSimulado = true;
                return Task.CompletedTask;
            }
        );

        Assert.True(viajeSimulado);
        Assert.NotNull(respuesta);
        Assert.EndsWith("\n", respuesta);

        using var doc = JsonDocument.Parse(respuesta);
        var root = doc.RootElement;

        Assert.Equal("ActualizarEstado", root.GetProperty("TipoMensaje").GetString());
        Assert.Equal(88, root.GetProperty("PedidoId").GetInt32());
        Assert.Equal("Entregado", root.GetProperty("Estado").GetString());
    }

    [Theory]
    [InlineData("{\"TipoMensaje\":\"PedidoEnViaje\", \"Id\": 88 ")]
    [InlineData("payload no valido")]
    [InlineData("{")]
    public async Task ProcesarMensaje_CU15_JSONCorrupto_NoLanzaExcepcionYRetornaNull(string jsonInvalido)
    {
        var respuesta = await DeliveryMessageProcessor.ProcesarMensajeAsync(
            jsonInvalido,
            simularViaje: () => Task.CompletedTask
        );

        Assert.Null(respuesta);
    }

    [Theory]
    [InlineData("{\"TipoMensaje\":\"OtroMensaje\"}")]
    [InlineData("{\"Tipo\":\"COCINA\"}")]
    [InlineData("{}")]
    public async Task ProcesarMensaje_CU15_MensajesNoRelevantes_RetornaNull(string jsonOtroTipo)
    {
        var respuesta = await DeliveryMessageProcessor.ProcesarMensajeAsync(
            jsonOtroTipo,
            simularViaje: () => Task.CompletedTask
        );

        Assert.Null(respuesta);
    }
}

