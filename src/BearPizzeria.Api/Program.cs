using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using BearPizzeria.Api.Data;
using BearPizzeria.Api.Models;
using BearPizzeria.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MySQL")
    ?? throw new InvalidOperationException("Connection string 'MySQL' not found");

builder.Services.AddDbContext<PedidoDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddSingleton<SocketServerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketServerService>());

builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/api/pizzas", async (PedidoDbContext db) =>
{
    logger.LogInformation("Consultando catálogo de pizzas");
    return await db.Pizzas.ToListAsync();
});

app.MapGet("/api/pizzas/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando pizza {Id}", id);
    return await db.Pizzas.FindAsync(id) is Pizza pizza
        ? Results.Ok(pizza)
        : Results.NotFound();
});

app.MapPost("/api/clientes", async (Cliente cliente, PedidoDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(cliente.Nombre))
        return Results.BadRequest("El nombre es obligatorio");
    if (string.IsNullOrWhiteSpace(cliente.Direccion))
        return Results.BadRequest("La dirección es obligatoria");
    if (string.IsNullOrWhiteSpace(cliente.Telefono))
        return Results.BadRequest("El teléfono es obligatorio");
    if (string.IsNullOrWhiteSpace(cliente.Email))
        return Results.BadRequest("El email es obligatorio");

    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    logger.LogInformation("Cliente registrado: {Id} - {Nombre}", cliente.Id, cliente.Nombre);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});

app.MapPost("/api/pedidos", async (CrearPedidoRequest request, PedidoDbContext db, SocketServerService socketServer) =>
{
    var cliente = await db.Clientes.FindAsync(request.ClienteId);
    if (cliente is null)
        return Results.BadRequest("Cliente no encontrado");

    if (request.Items is null || request.Items.Count == 0)
        return Results.BadRequest("El pedido debe tener al menos una pizza");

    if (request.Items.Any(i => i.Cantidad <= 0))
        return Results.BadRequest("Las cantidades deben ser mayores a cero");

    var pizzaIds = request.Items.Select(i => i.PizzaId).ToList();
    var pizzas = await db.Pizzas.Where(p => pizzaIds.Contains(p.Id)).ToListAsync();

    if (pizzas.Count != pizzaIds.Count)
        return Results.BadRequest("Una o más pizzas no existen");

    var pedido = new Pedido
    {
        ClienteId = request.ClienteId,
        Estado = EstadoPedido.EsperaDeConfirmacion,
        PedidoPizzas = request.Items.Select(item =>
        {
            var pizza = pizzas.First(p => p.Id == item.PizzaId);
            return new PedidoPizza
            {
                PizzaId = item.PizzaId,
                Cantidad = item.Cantidad,
                PrecioUnitario = pizza.Precio
            };
        }).ToList()
    };

    pedido.Total = pedido.PedidoPizzas.Sum(pp => pp.Cantidad * pp.PrecioUnitario);

    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync();

    logger.LogInformation("Pedido creado: #{Id} - Cliente: {Cliente} - Total: {Total}",
        pedido.Id, cliente.Nombre, pedido.Total);

    await socketServer.NotificarNuevoPedidoAsync(pedido);

    return Results.Created($"/api/pedidos/{pedido.Id}", pedido);
});

app.MapGet("/api/pedidos/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando pedido #{Id}", id);
    return await db.Pedidos
        .Include(p => p.Cliente)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstOrDefaultAsync(p => p.Id == id) is Pedido pedido
            ? Results.Ok(pedido)
            : Results.NotFound();
});

app.MapPatch("/api/pedidos/{id}/estado", async (int id, ActualizarEstadoRequest request, PedidoDbContext db) =>
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null)
        return Results.NotFound();

    if (!Enum.TryParse<EstadoPedido>(request.Estado, out var nuevoEstado))
        return Results.BadRequest("Estado inválido. Valores válidos: EsperaDeConfirmacion, EnPreparacion, EnViaje, Entregado");

    logger.LogInformation("Pedido #{Id}: {EstadoAnterior} -> {EstadoNuevo}", id, pedido.Estado, nuevoEstado);
    pedido.Estado = nuevoEstado;
    await db.SaveChangesAsync();

    return Results.Ok(pedido);
});

app.Run();

public class CrearPedidoRequest
{
    public int ClienteId { get; set; }
    public List<PedidoItemRequest> Items { get; set; } = [];
}

public class PedidoItemRequest
{
    public int PizzaId { get; set; }
    public int Cantidad { get; set; } = 1;
}

public class ActualizarEstadoRequest
{
    public string Estado { get; set; } = string.Empty;
}
