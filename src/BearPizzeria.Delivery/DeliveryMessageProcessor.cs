using System.Text.Json;

namespace BearPizzeria.Delivery;

public static class DeliveryMessageProcessor
{
    public const string HandshakeTipo = "REPARTO";

    public static string CreateHandshake() =>
        JsonSerializer.Serialize(new { Tipo = HandshakeTipo }) + "\n";

    public static async Task<string?> ProcesarMensajeAsync(string mensaje, Func<Task>? simularViaje = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(mensaje);
            var root = doc.RootElement;

            if (root.TryGetProperty("TipoMensaje", out var tipoMsg))
            {
                var tipo = tipoMsg.GetString();
                if (tipo == "PedidoEnViaje")
                {
                    var pedidoId = root.GetProperty("Id").GetInt32();
                    Console.WriteLine($"========================================");
                    Console.WriteLine($"PEDIDO EN VIAJE #{pedidoId}");
                    Console.WriteLine($"========================================");

                    if (root.TryGetProperty("Detalle", out var detalle))
                    {
                        if (detalle.TryGetProperty("Cliente", out var cli))
                            Console.WriteLine($"Cliente: {cli.GetString()}");
                        if (detalle.TryGetProperty("Direccion", out var dir))
                            Console.WriteLine($"Dirección: {dir.GetString()}");
                    }

                    Console.WriteLine($"\n[REPARTO] Pedido #{pedidoId} en camino a deliver...");
                    Console.WriteLine("[REPARTO] Conductor asignado. Viajando al domicilio...");
                    if (simularViaje is not null)
                    {
                        await simularViaje();
                    }
                    else
                    {
                        await Task.Delay(5000);
                    }
                    Console.WriteLine("[REPARTO] Pedido entregado al cliente!\n");

                    return JsonSerializer.Serialize(new
                    {
                        TipoMensaje = "ActualizarEstado",
                        PedidoId = pedidoId,
                        Estado = "Entregado"
                    }) + "\n";
                }
            }

            return null;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error decodificando mensaje: {ex.Message}");
            return null;
        }
    }
}

