using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BearPizzeria.Api.Data;
using BearPizzeria.Api.Models;
using BearPizzeria.Api.Services;
using BearPizzeria.Common.Testing;
using Xunit;

using Xunit.Abstractions;

namespace BearPizzeria.Sockets.Tests;

public class SocketServerIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private SqliteConnection _sqliteConnection = null!;
    private ServiceProvider _serviceProvider = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private SocketServerService _server = null!;
    private CancellationTokenSource _serverCts = null!;
    private Task _serverTask = null!;

    public SocketServerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        SocketServerService.DebugLog = msg => _output.WriteLine(msg);

        _sqliteConnection = new SqliteConnection("DataSource=:memory:;Mode=Memory;Cache=Shared");
        await _sqliteConnection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<PizzeriaDbContext>(options =>
            options.UseSqlite(_sqliteConnection));
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var logger = loggerFactory.CreateLogger<SocketServerService>();

        _server = new SocketServerService(_scopeFactory, logger, 0);
        _serverCts = new CancellationTokenSource();
        _serverTask = _server.StartAsync(_serverCts.Token);

        // Esperar a que el servidor asigne el puerto efímero y comience a escuchar
        for (int i = 0; i < 100 && _server.PuertoActual == 0; i++)
        {
            await Task.Delay(50);
        }
        _output.WriteLine($"Server PuertoActual: {_server.PuertoActual}");
    }

    public async Task DisposeAsync()
    {
        _serverCts.Cancel();
        try
        {
            await _server.StopAsync(CancellationToken.None);
        }
        catch
        {
        }

        try
        {
            await _serverTask;
        }
        catch
        {
        }

        await _sqliteConnection.DisposeAsync();
        await _serviceProvider.DisposeAsync();
        _serverCts.Dispose();
    }

    private async Task<(TcpClient Client, StreamReader Reader, StreamWriter Writer)> ConnectClientAsync()
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, _server.PuertoActual);
        var stream = client.GetStream();
        var encoding = new UTF8Encoding(false);
        var reader = new StreamReader(stream, encoding);
        var writer = new StreamWriter(stream, encoding) { AutoFlush = true };
        return (client, reader, writer);
    }

    private static async Task<string?> ReadLineWithTimeoutAsync(StreamReader reader, int timeoutMs = 5000)
    {
        var readTask = reader.ReadLineAsync();
        var delayTask = Task.Delay(timeoutMs);
        var completed = await Task.WhenAny(readTask, delayTask);
        if (completed == delayTask)
        {
            throw new TimeoutException($"Tiempo agotado ({timeoutMs}ms) esperando datos en el socket.");
        }
        return await readTask;
    }

    private async Task<(Cliente cliente, Pizza pizza, Pedido pedido)> SeedPedidoAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();

        var (cliente, _, _) = await TestDatabaseFactory.SeedClienteAsync(
            db,
            nombre: "Carlos Gomez",
            direccion: "Calle Falsa 123",
            telefono: "+54 11 5555-4321");

        var pizza = new Pizza
        {
            Nombre = "Fugazzeta Rellena",
            Descripcion = "Cebolla y abundante queso",
            Precio = 15000m,
            Stock = 10,
            Tamano = TamanoPizza.Grande
        };
        db.Pizzas.Add(pizza);
        await db.SaveChangesAsync();

        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            Estado = EstadoPedido.EnPreparacion,
            FechaPedido = DateTime.UtcNow,
            Total = 15000m,
            DireccionEntrega = "Calle Falsa 123"
        };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();

        var pedidoPizza = new PedidoPizza
        {
            PedidoId = pedido.Id,
            PizzaId = pizza.Id,
            Cantidad = 1,
            Tamano = TamanoPizza.Grande,
            PrecioUnitario = 15000m,
            Subtotal = 15000m
        };
        db.PedidoPizzas.Add(pedidoPizza);
        await db.SaveChangesAsync();

        return (cliente, pizza, pedido);
    }

    [Fact]
    public async Task Handshake_CocinaYReparto_SeRegistranCorrectamente()
    {
        // Arrange
        _output.WriteLine($"Connecting cocina to {_server.PuertoActual}");
        var (cocina, _, cocinaWriter) = await ConnectClientAsync();
        _output.WriteLine($"Cocina connected: {cocina.Connected}");
        var (reparto, _, repartoWriter) = await ConnectClientAsync();
        _output.WriteLine($"Reparto connected: {reparto.Connected}");

        try
        {
            // Act
            await cocinaWriter.WriteLineAsync("{\"Tipo\":\"COCINA\"}");
            await cocinaWriter.FlushAsync();
            await repartoWriter.WriteLineAsync("{\"Tipo\":\"REPARTO\"}");
            await repartoWriter.FlushAsync();

            // Esperar que el servidor procese ambos mensajes
            await Task.Delay(300);

            _output.WriteLine($"Kitchen count: {_server.KitchenClientsCount}, Delivery count: {_server.DeliveryClientsCount}");

            // Assert
            Assert.Equal(1, _server.KitchenClientsCount);
            Assert.Equal(1, _server.DeliveryClientsCount);
        }
        finally
        {
            cocina.Close();
            reparto.Close();
        }
    }

    [Fact]
    public async Task FlujoCompleto_NotificarPedido_CocinaRecibe_ActualizaEnViaje_RepartoRecibe_ActualizaEntregado()
    {
        // Arrange
        var (_, _, pedido) = await SeedPedidoAsync();

        var (cocina, cocinaReader, cocinaWriter) = await ConnectClientAsync();
        var (reparto, repartoReader, repartoWriter) = await ConnectClientAsync();

        try
        {
            // Handshake
            await cocinaWriter.WriteLineAsync("{\"Tipo\":\"COCINA\"}");
            await repartoWriter.WriteLineAsync("{\"Tipo\":\"REPARTO\"}");
            await Task.Delay(200);

            // 1. Servidor notifica nuevo pedido a la cola
            await _server.NotificarNuevoPedidoAsync(pedido);

            // 2. Cocina debe recibir el mensaje "NuevoPedido"
            var mensajeCocina = await ReadLineWithTimeoutAsync(cocinaReader);
            Assert.NotNull(mensajeCocina);

            using var docCocina = JsonDocument.Parse(mensajeCocina!);
            var rootCocina = docCocina.RootElement;
            Assert.Equal("NuevoPedido", rootCocina.GetProperty("TipoMensaje").GetString());
            Assert.Equal(pedido.Id, rootCocina.GetProperty("Id").GetInt32());

            var detalle = rootCocina.GetProperty("Detalle");
            Assert.Equal("Carlos Gomez", detalle.GetProperty("Cliente").GetString());
            Assert.Equal("Calle Falsa 123", detalle.GetProperty("Direccion").GetString());

            // 3. Cocina responde actualizando estado a EnViaje
            var actualizacionCocina = JsonSerializer.Serialize(new
            {
                TipoMensaje = "ActualizarEstado",
                PedidoId = pedido.Id,
                Estado = "EnViaje"
            });
            await cocinaWriter.WriteLineAsync(actualizacionCocina);

            // 4. Reparto debe recibir automáticamente la notificación "PedidoEnViaje"
            var mensajeReparto = await ReadLineWithTimeoutAsync(repartoReader);
            Assert.NotNull(mensajeReparto);

            using var docReparto = JsonDocument.Parse(mensajeReparto!);
            var rootReparto = docReparto.RootElement;
            Assert.Equal("PedidoEnViaje", rootReparto.GetProperty("TipoMensaje").GetString());
            Assert.Equal(pedido.Id, rootReparto.GetProperty("Id").GetInt32());

            // Verificar en BD que el pedido cambió a EnViaje
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
                var pedidoDb = await db.Pedidos.FindAsync(pedido.Id);
                Assert.NotNull(pedidoDb);
                Assert.Equal(EstadoPedido.EnViaje, pedidoDb!.Estado);
            }

            // 5. Reparto responde actualizando estado a Entregado
            var actualizacionReparto = JsonSerializer.Serialize(new
            {
                TipoMensaje = "ActualizarEstado",
                PedidoId = pedido.Id,
                Estado = "Entregado"
            });
            await repartoWriter.WriteLineAsync(actualizacionReparto);
            await Task.Delay(300);

            // Verificar en BD que el pedido finalmente pasó a Entregado
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
                var pedidoDb = await db.Pedidos.FindAsync(pedido.Id);
                Assert.NotNull(pedidoDb);
                Assert.Equal(EstadoPedido.Entregado, pedidoDb!.Estado);
            }
        }
        finally
        {
            cocina.Close();
            reparto.Close();
        }
    }

    [Fact]
    public async Task DesconexionCliente_RemueveDeColeccionSinAfectarOtrosSockets()
    {
        // Arrange
        var (cocina, _, cocinaWriter) = await ConnectClientAsync();
        var (reparto, _, repartoWriter) = await ConnectClientAsync();

        try
        {
            await cocinaWriter.WriteLineAsync("{\"Tipo\":\"COCINA\"}");
            await repartoWriter.WriteLineAsync("{\"Tipo\":\"REPARTO\"}");
            await Task.Delay(200);

            Assert.Equal(1, _server.KitchenClientsCount);
            Assert.Equal(1, _server.DeliveryClientsCount);

            // Act: Cocina se desconecta intempestivamente
            cocina.Close();

            // Esperar que el servidor detecte lectura 0 o desconexión
            await Task.Delay(400);

            // Assert
            Assert.Equal(0, _server.KitchenClientsCount);
            Assert.Equal(1, _server.DeliveryClientsCount); // Reparto sigue intacto
        }
        finally
        {
            reparto.Close();
        }
    }

    [Fact]
    public async Task MensajeInvalidoOMalformado_NoProduceCrashEnServidor()
    {
        // Arrange
        var (cocina, cocinaReader, cocinaWriter) = await ConnectClientAsync();
        var (_, _, pedido) = await SeedPedidoAsync();

        try
        {
            // Registrar cocina
            await cocinaWriter.WriteLineAsync("{\"Tipo\":\"COCINA\"}");
            await Task.Delay(100);

            // Act: Enviar JSON trunco/malformado
            await cocinaWriter.WriteLineAsync("{\"TipoMensaje\":\"ActualizarEstado\", \"PedidoId\": ");
            await Task.Delay(200);

            // Act: Enviar tipo desconocido
            await cocinaWriter.WriteLineAsync("{\"TipoMensaje\":\"MensajeDesconocidoInexistente\"}");
            await Task.Delay(200);

            // Enviar pedido válido para confirmar que el servidor sigue funcionando normalmente
            await _server.NotificarNuevoPedidoAsync(pedido);
            var mensaje = await ReadLineWithTimeoutAsync(cocinaReader);

            // Assert
            Assert.NotNull(mensaje);
            using var doc = JsonDocument.Parse(mensaje!);
            Assert.Equal("NuevoPedido", doc.RootElement.GetProperty("TipoMensaje").GetString());
        }
        finally
        {
            cocina.Close();
        }
    }

    [Fact]
    public async Task ActualizarEstado_PedidoInexistente_ManejaSinExcepcion()
    {
        // Arrange
        var (cocina, _, cocinaWriter) = await ConnectClientAsync();

        try
        {
            await cocinaWriter.WriteLineAsync("{\"Tipo\":\"COCINA\"}");
            await Task.Delay(100);

            // Act: Intentar actualizar pedido inexistente (Id: 99999)
            var actualizacionInvalida = JsonSerializer.Serialize(new
            {
                TipoMensaje = "ActualizarEstado",
                PedidoId = 99999,
                Estado = "EnViaje"
            });
            await cocinaWriter.WriteLineAsync(actualizacionInvalida);
            await Task.Delay(200);

            // Assert: Servidor sigue activo y no arrojó crash
            Assert.Equal(1, _server.KitchenClientsCount);
        }
        finally
        {
            cocina.Close();
        }
    }

    [Fact]
    public async Task ActualizarEstado_EstadoInvalido_IgnoraSinModificarBD()
    {
        // Arrange
        var (_, _, pedido) = await SeedPedidoAsync();
        var (cocina, _, cocinaWriter) = await ConnectClientAsync();

        try
        {
            await cocinaWriter.WriteLineAsync("{\"Tipo\":\"COCINA\"}");
            await Task.Delay(100);

            // Act: Enviar estado no existente en el enum
            var actualizacionInvalida = JsonSerializer.Serialize(new
            {
                TipoMensaje = "ActualizarEstado",
                PedidoId = pedido.Id,
                Estado = "EstadoInexistenteInventado"
            });
            await cocinaWriter.WriteLineAsync(actualizacionInvalida);
            await Task.Delay(200);

            // Assert: En BD el pedido sigue en estado EnPreparacion
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            var pedidoDb = await db.Pedidos.FindAsync(pedido.Id);
            Assert.NotNull(pedidoDb);
            Assert.Equal(EstadoPedido.EnPreparacion, pedidoDb!.Estado);
        }
        finally
        {
            cocina.Close();
        }
    }

    [Fact]
    public async Task NotificarPedidoEnViajeAsync_Directo_EnviaAReparto()
    {
        // Arrange
        var (_, _, pedido) = await SeedPedidoAsync();
        var (reparto, repartoReader, repartoWriter) = await ConnectClientAsync();

        try
        {
            await repartoWriter.WriteLineAsync("{\"Tipo\":\"REPARTO\"}");
            await Task.Delay(150);

            // Act
            await _server.NotificarPedidoEnViajeAsync(pedido.Id);

            // Assert: Reparto recibe PedidoEnViaje
            var mensaje = await ReadLineWithTimeoutAsync(repartoReader);
            Assert.NotNull(mensaje);
            using var doc = JsonDocument.Parse(mensaje!);
            Assert.Equal("PedidoEnViaje", doc.RootElement.GetProperty("TipoMensaje").GetString());
            Assert.Equal(pedido.Id, doc.RootElement.GetProperty("Id").GetInt32());
        }
        finally
        {
            reparto.Close();
        }
    }

    [Fact]
    public async Task NotificarNuevoPedidoAsync_SinCocinaConectada_ProcesaCanalSinFallo()
    {
        // Arrange
        var (_, _, pedido) = await SeedPedidoAsync();

        // Act: Enviar notificación sin ningún cliente conectado
        await _server.NotificarNuevoPedidoAsync(pedido);
        await Task.Delay(200);

        // Assert: El servidor sigue saludable con 0 clientes conectados
        Assert.Equal(0, _server.KitchenClientsCount);
        Assert.Equal(0, _server.DeliveryClientsCount);
    }

    [Fact]
    public void RemoverCliente_ClienteDesconocido_NoLanzaExcepcion()
    {
        // Arrange
        using var dummyClient = new TcpClient();

        // Act & Assert
        var ex = Record.Exception(() => _server.RemoverCliente(dummyClient));
        Assert.Null(ex);
        Assert.Equal(0, _server.KitchenClientsCount);
        Assert.Equal(0, _server.DeliveryClientsCount);
    }
}
