using System.Text.Json;

namespace BearPizzeria.Kitchen;

public static class KitchenMessageProcessor
{
    public const string HandshakeTipo = "COCINA";

    public static string CreateHandshake() =>
        JsonSerializer.Serialize(new { Tipo = HandshakeTipo }) + "\n";

    public static async Task<string?> ProcesarMensajeAsync(string mensaje, Func<Task>? simularCoccion = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(mensaje);
            var root = doc.RootElement;

            if (root.TryGetProperty("TipoMensaje", out var tipoMsg))
            {
                var tipo = tipoMsg.GetString();
                if (tipo == "NuevoPedido")
                {
                    var pedidoId = root.GetProperty("Id").GetInt32();
                    Console.WriteLine($"========================================");
                    Console.WriteLine($"NUEVO PEDIDO #{pedidoId}");
                    Console.WriteLine($"========================================");

                    if (root.TryGetProperty("Detalle", out var detalle))
                    {
                        if (detalle.TryGetProperty("Cliente", out var cli))
                            Console.WriteLine($"Cliente: {cli.GetString()}");
                        if (detalle.TryGetProperty("Direccion", out var dir))
                            Console.WriteLine($"Dirección: {dir.GetString()}");
                        Console.WriteLine("Items:");

                        if (detalle.TryGetProperty("Items", out var itemsElem) && itemsElem.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in itemsElem.EnumerateArray())
                            {
                                var cant = item.GetProperty("Cantidad").GetInt32();
                                var pizza = item.GetProperty("Pizza").GetString();
                                var precio = item.GetProperty("PrecioUnitario").GetDecimal();
                                Console.WriteLine($"  - {cant}x {pizza} (${precio})");
                            }
                        }

                        if (detalle.TryGetProperty("Total", out var tot))
                            Console.WriteLine($"Total: ${tot.GetDecimal()}");
                    }

                    Console.WriteLine("\n[COCINA] Preparando pedido...");
                    if (simularCoccion is not null)
                    {
                        await simularCoccion();
                    }
                    else
                    {
                        await Task.Delay(5000);
                    }
                    Console.WriteLine("[COCINA] Pedido listo para reparto!\n");

                    return JsonSerializer.Serialize(new
                    {
                        TipoMensaje = "ActualizarEstado",
                        PedidoId = pedidoId,
                        Estado = "EnViaje"
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

