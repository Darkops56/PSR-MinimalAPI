using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BearPizzeria.Api.Data;
using BearPizzeria.Api.Models;

namespace BearPizzeria.Api.Services;

public class SocketServerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SocketServerService> _logger;
    private readonly Channel<Pedido> _pedidoChannel = Channel.CreateUnbounded<Pedido>();

    private readonly ConcurrentDictionary<string, TcpClient> _kitchenClients = new();
    private readonly ConcurrentDictionary<string, TcpClient> _deliveryClients = new();

    private const int Puerto = 5050;

    public SocketServerService(IServiceScopeFactory scopeFactory, ILogger<SocketServerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task NotificarNuevoPedidoAsync(Pedido pedido)
    {
        await _pedidoChannel.Writer.WriteAsync(pedido);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SocketServer iniciado en puerto {Puerto}", Puerto);

        var listener = new TcpListener(IPAddress.Any, Puerto);
        listener.Start();

        var procesarPedidos = ProcesarPedidosChannelAsync(stoppingToken);

        await AcceptClientsAsync(listener, stoppingToken);

        await procesarPedidos;
    }

    private async Task AcceptClientsAsync(TcpListener listener, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aceptando conexión");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken stoppingToken)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "desconocido";
        var clientId = $"{remoteEndPoint}";
        _logger.LogInformation("Cliente conectado: {ClientId}", clientId);

        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];
            var mensajeParcial = new StringBuilder();
            var registrado = false;

            while (!stoppingToken.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
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
                        registrado = await ProcesarMensajeAsync(linea, client, stream, stoppingToken) || registrado;
                    }
                }

                mensajeParcial.Clear();
                mensajeParcial.Append(mensajeCompleto);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Cliente desconectado: {ClientId}", clientId);
        }
        finally
        {
            RemoverCliente(client);
            client.Close();
        }
    }

    private void RemoverCliente(TcpClient client)
    {
        try
        {
            var endPoint = client.Client.RemoteEndPoint?.ToString();

            if (endPoint is null)
            {
                _kitchenClients.Clear();
                _deliveryClients.Clear();
                return;
            }

            var keyK = _kitchenClients.FirstOrDefault(x =>
                x.Value.Client.RemoteEndPoint?.ToString() == endPoint).Key;
            if (keyK is not null)
            {
                _kitchenClients.TryRemove(keyK, out _);
                _logger.LogInformation("Cocina removida: {EndPoint}", endPoint);
                return;
            }

            var keyD = _deliveryClients.FirstOrDefault(x =>
                x.Value.Client.RemoteEndPoint?.ToString() == endPoint).Key;
            if (keyD is not null)
            {
                _deliveryClients.TryRemove(keyD, out _);
                _logger.LogInformation("Reparto removido: {EndPoint}", endPoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al remover cliente");
        }
    }

    private async Task<bool> ProcesarMensajeAsync(string mensaje, TcpClient client, NetworkStream stream, CancellationToken stoppingToken)
    {
        try
        {
            using var doc = JsonDocument.Parse(mensaje);
            var root = doc.RootElement;

            if (root.TryGetProperty("Tipo", out var tipoElem))
            {
                var tipo = tipoElem.GetString();
                var endPoint = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

                if (tipo == "COCINA")
                {
                    _kitchenClients.TryAdd(endPoint, client);
                    _logger.LogInformation("Cocina registrada: {EndPoint}", endPoint);
                    return true;
                }
                if (tipo == "REPARTO")
                {
                    _deliveryClients.TryAdd(endPoint, client);
                    _logger.LogInformation("Reparto registrado: {EndPoint}", endPoint);
                    return true;
                }
            }

            if (root.TryGetProperty("TipoMensaje", out var tipoMsgElem))
            {
                var tipoMsg = tipoMsgElem.GetString();
                if (tipoMsg == "ActualizarEstado")
                {
                    var pedidoId = root.GetProperty("PedidoId").GetInt32();
                    var estadoStr = root.GetProperty("Estado").GetString();

                    if (Enum.TryParse<EstadoPedido>(estadoStr, out var nuevoEstado))
                    {
                        await ActualizarEstadoPedidoAsync(pedidoId, nuevoEstado);
                        _logger.LogInformation("Pedido {Id} actualizado a {Estado}", pedidoId, nuevoEstado);

                        if (nuevoEstado == EstadoPedido.EnViaje)
                        {
                            await EnviarADeliveryAsync(pedidoId, stoppingToken);
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Mensaje JSON inválido recibido");
        }

        return false;
    }

    private async Task ActualizarEstadoPedidoAsync(int pedidoId, EstadoPedido nuevoEstado)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidoDbContext>();

        var pedido = await db.Pedidos.FindAsync(pedidoId);
        if (pedido is not null)
        {
            pedido.Estado = nuevoEstado;
            await db.SaveChangesAsync();
        }
    }

    private async Task ProcesarPedidosChannelAsync(CancellationToken stoppingToken)
    {
        await foreach (var pedido in _pedidoChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PedidoDbContext>();

            var pedidoCompleto = await db.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.PedidoPizzas)
                    .ThenInclude(pp => pp.Pizza)
                .FirstOrDefaultAsync(p => p.Id == pedido.Id, stoppingToken);

            if (pedidoCompleto is null) continue;

            var mensaje = JsonSerializer.Serialize(new
            {
                TipoMensaje = "NuevoPedido",
                pedidoCompleto.Id,
                Detalle = new
                {
                    Cliente = pedidoCompleto.Cliente.Nombre,
                    Direccion = pedidoCompleto.Cliente.Direccion,
                    Items = pedidoCompleto.PedidoPizzas.Select(pp => new
                    {
                        Pizza = pp.Pizza.Nombre,
                        pp.Cantidad,
                        pp.PrecioUnitario
                    }),
                    Total = pedidoCompleto.Total
                }
            });

            await EnviarATodosAsync(_kitchenClients, mensaje, stoppingToken);
        }
    }

    private async Task EnviarADeliveryAsync(int pedidoId, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PedidoDbContext>();

        var pedido = await db.Pedidos
            .Include(p => p.Cliente)
            .FirstOrDefaultAsync(p => p.Id == pedidoId, stoppingToken);

        if (pedido is null) return;

        var mensaje = JsonSerializer.Serialize(new
        {
            TipoMensaje = "PedidoEnViaje",
            pedido.Id,
            Detalle = new
            {
                Cliente = pedido.Cliente.Nombre,
                Direccion = pedido.Cliente.Direccion
            }
        });

        await EnviarATodosAsync(_deliveryClients, mensaje, stoppingToken);
    }

    private async Task EnviarATodosAsync(ConcurrentDictionary<string, TcpClient> clients, string mensaje, CancellationToken stoppingToken)
    {
        var data = Encoding.UTF8.GetBytes(mensaje + "\n");
        var desconectados = new List<string>();

        foreach (var (id, client) in clients)
        {
            try
            {
                if (client.Connected)
                {
                    await client.GetStream().WriteAsync(data, 0, data.Length, stoppingToken);
                }
                else
                {
                    desconectados.Add(id);
                }
            }
            catch (Exception)
            {
                desconectados.Add(id);
            }
        }

        foreach (var id in desconectados)
        {
            clients.TryRemove(id, out _);
            _logger.LogWarning("Cliente removido por desconexión: {Id}", id);
        }
    }
}
