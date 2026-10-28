using System.Net.Sockets;
using System.Text;
using BearPizzeria.Kitchen;

const string Host = "localhost";
const int Puerto = 5050;

Console.WriteLine("=== COCINA AUTOMATIZADA ===");

while (true)
{
    try
    {
        Console.WriteLine("Conectando al servidor de pedidos...");
        using var client = new TcpClient();
        await client.ConnectAsync(Host, Puerto);
        Console.WriteLine("Conectado al backend. Esperando pedidos...\n");

        using var stream = client.GetStream();

        var identificacion = KitchenMessageProcessor.CreateHandshake();
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
                    var respuesta = await KitchenMessageProcessor.ProcesarMensajeAsync(linea);
                    if (respuesta is not null)
                    {
                        var data = Encoding.UTF8.GetBytes(respuesta);
                        await stream.WriteAsync(data);
                        Console.WriteLine($"[COCINA] Respuesta enviada al backend.\n");
                    }
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
