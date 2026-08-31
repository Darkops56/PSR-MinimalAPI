using System.Net.Sockets;
using System.Text;
using System.Text.Json;

const string Host = "localhost";
const int Puerto = 5050;

Console.WriteLine("=== REPARTO AUTOMATIZADO ===");

while (true)
{
    try
    {
        Console.WriteLine("Conectando al servidor de pedidos...");
        using var client = new TcpClient();
        await client.ConnectAsync(Host, Puerto);
        Console.WriteLine("Conectado al backend. Esperando pedidos para entregar...\n");

        using var stream = client.GetStream();

        var identificacion = JsonSerializer.Serialize(new { Tipo = "REPARTO" }) + "\n";
        var idBytes = Encoding.UTF8.GetBytes(identificacion);
        await stream.WriteAsync(idBytes);

        var buffer = new byte[4096];
        var mensajeParcial = new StringBuilder();

        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0) break;

            mensajeParcial.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            var mensajeCompleto = mensajeParcial.ToString();

            while (mensajeCompleto.Contains('\n'))
            {
                var idx = mensajeCompleto.IndexOf('\n');
                var linea = mensajeCompleto[..idx];
                mensajeCompleto = mensajeCompleto[(idx + 1)..];

                if (!string.IsNullOrWhiteSpace(linea))
                {
                    await ProcesarMensaje(linea, stream);
                }
            }

            mensajeParcial.Clear();
            mensajeParcial.Append(mensajeCompleto);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError de conexión: {ex.Message}");
    }

    Console.WriteLine("Reconectando en 5 segundos...");
    await Task.Delay(5000);
}

static async Task ProcesarMensaje(string mensaje, NetworkStream stream)
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

                var detalle = root.GetProperty("Detalle");
                Console.WriteLine($"Cliente: {detalle.GetProperty("Cliente").GetString()}");
                Console.WriteLine($"Dirección: {detalle.GetProperty("Direccion").GetString()}");

                Console.WriteLine($"\n[REPARTO] Pedido #{pedidoId} en camino a deliver...");
                Console.WriteLine("[REPARTO] Conductor asignado. Viajando al domicilio...");
                await Task.Delay(3000);
                Console.WriteLine("[REPARTO] Pedido entregado al cliente!\n");

                var respuesta = JsonSerializer.Serialize(new
                {
                    TipoMensaje = "ActualizarEstado",
                    PedidoId = pedidoId,
                    Estado = "Entregado"
                }) + "\n";

                var data = Encoding.UTF8.GetBytes(respuesta);
                await stream.WriteAsync(data);

                Console.WriteLine($"[REPARTO] Pedido #{pedidoId} marcado como 'Entregado'\n");
            }
        }
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error decodificando mensaje: {ex.Message}");
    }
}
