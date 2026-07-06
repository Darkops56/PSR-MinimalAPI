using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using BearPizzeria.Api.Data;
using BearPizzeria.Api.Models;
using BearPizzeria.Api.Models.DTOs;
using BearPizzeria.Api.Services;
using BearPizzeria.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MySQL")
    ?? throw new InvalidOperationException("Connection string 'MySQL' not found");

builder.Services.AddDbContext<PedidoDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddSingleton<SocketServerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketServerService>());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddValidatorsFromAssemblyContaining<ClienteValidator>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BearPizzeria API";
        document.Info.Version = "v1";
        document.Info.Description = "API de gestión de pedidos de pizzas";
        return Task.CompletedTask;
    });
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("BearPizzeria API")
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapGet("/api/pizzas", async (PedidoDbContext db) =>
{
    logger.LogInformation("Consultando catálogo de pizzas");
    var pizzas = await db.Pizzas.ToListAsync();
    return Results.Ok(pizzas);
});

app.MapGet("/api/pizzas/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando pizza {Id}", id);
    return await db.Pizzas.FindAsync(id) is Pizza pizza
        ? Results.Ok(pizza)
        : Results.NotFound(new ErrorResponse("PIZZA-404", $"Pizza con ID {id} no encontrada"));
});

app.MapPost("/api/clientes", async (Cliente cliente, PedidoDbContext db, IValidator<Cliente> validator) =>
{
    var validationResult = await validator.ValidateAsync(cliente);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("CLIENTE-400", "No se pudo registrar el cliente. Corregí los campos indicados.", errores));
    }

    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    logger.LogInformation("Cliente registrado: {Id} - {Nombre}", cliente.Id, cliente.Nombre);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
});

app.MapPost("/api/pedidos", async (CrearPedidoRequest request, PedidoDbContext db, SocketServerService socketServer, IValidator<CrearPedidoRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("PEDIDO-400", "No se pudo crear el pedido. Corregí los datos.", errores));
    }

    var cliente = await db.Clientes.FindAsync(request.ClienteId);
    if (cliente is null)
        return Results.BadRequest(new ErrorResponse("PEDIDO-404", $"Cliente con ID {request.ClienteId} no encontrado"));

    var pizzaIds = request.Items.Select(i => i.PizzaId).ToList();
    var pizzas = await db.Pizzas.Where(p => pizzaIds.Contains(p.Id)).ToListAsync();

    if (pizzas.Count != pizzaIds.Count)
    {
        var idsExistentes = pizzas.Select(p => p.Id).ToHashSet();
        var idsInvalidos = pizzaIds.Where(id => !idsExistentes.Contains(id));
        return Results.BadRequest(new ErrorResponse("PEDIDO-400", $"Las pizzas con IDs {string.Join(", ", idsInvalidos)} no existen"));
    }

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
            : Results.NotFound(new ErrorResponse("PEDIDO-404", $"Pedido con ID {id} no encontrado"));
});

app.MapPatch("/api/pedidos/{id}/estado", async (int id, ActualizarEstadoRequest request, PedidoDbContext db, IValidator<ActualizarEstadoRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("ESTADO-400", "No se pudo actualizar el estado. Corregí el dato.", errores));
    }

    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null)
        return Results.NotFound(new ErrorResponse("PEDIDO-404", $"Pedido con ID {id} no encontrado"));

    var nuevoEstado = Enum.Parse<EstadoPedido>(request.Estado);

    logger.LogInformation("Pedido #{Id}: {EstadoAnterior} -> {EstadoNuevo}", id, pedido.Estado, nuevoEstado);
    pedido.Estado = nuevoEstado;
    await db.SaveChangesAsync();

    return Results.Ok(pedido);
});

app.Run();

public class ErrorResponse
{
    public string Codigo { get; set; }
    public string Mensaje { get; set; }
    public List<ErrorDetalle>? Errores { get; set; }

    public ErrorResponse(string codigo, string mensaje, List<ErrorDetalle>? errores = null)
    {
        Codigo = codigo;
        Mensaje = mensaje;
        Errores = errores;
    }
}

public class ErrorDetalle
{
    public string Campo { get; set; }
    public string Mensaje { get; set; }

    public ErrorDetalle(string campo, string mensaje)
    {
        Campo = campo;
        Mensaje = mensaje;
    }
}
