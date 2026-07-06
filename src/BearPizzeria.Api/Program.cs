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

app.MapGet("/api/clientes", async (PedidoDbContext db) =>
{
    logger.LogInformation("Consultando lista de clientes");
    var clientes = await db.Clientes.ToListAsync();
    return Results.Ok(clientes.Select(c => c.ToResponse()).ToList());
});

app.MapGet("/api/clientes/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando cliente {Id}", id);
    return await db.Clientes.FindAsync(id) is Cliente cliente
        ? Results.Ok(cliente.ToResponse())
        : Results.NotFound(new ErrorResponse("CLIENTE-404", $"Cliente con ID {id} no encontrado"));
});

app.MapPost("/api/clientes", async (CrearClienteRequest request, PedidoDbContext db, IValidator<CrearClienteRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("CLIENTE-400", "No se pudo registrar el cliente. Corregí los campos indicados.", errores));
    }

    var cliente = new Cliente
    {
        Nombre = request.Nombre,
        Direccion = request.Direccion,
        Telefono = request.Telefono,
        Email = request.Email
    };

    db.Clientes.Add(cliente);
    await db.SaveChangesAsync();
    logger.LogInformation("Cliente registrado: {Id} - {Nombre}", cliente.Id, cliente.Nombre);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente.ToResponse());
});

app.MapDelete("/api/clientes/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Eliminando cliente {Id}", id);
    var cliente = await db.Clientes.FindAsync(id);
    if (cliente is null)
        return Results.NotFound(new ErrorResponse("CLIENTE-404", $"Cliente con ID {id} no encontrado"));

    var tienePedidos = await db.Pedidos.AnyAsync(p => p.ClienteId == id);
    if (tienePedidos)
        return Results.BadRequest(new ErrorResponse("CLIENTE-409", $"No se puede eliminar el cliente {id} porque tiene pedidos asociados."));

    db.Clientes.Remove(cliente);
    await db.SaveChangesAsync();
    logger.LogInformation("Cliente eliminado: {Id}", id);
    return Results.NoContent();
});

app.MapGet("/api/pizzas", async (PedidoDbContext db) =>
{
    logger.LogInformation("Consultando catálogo de pizzas");
    var pizzas = await db.Pizzas.ToListAsync();
    return Results.Ok(pizzas.Select(p => p.ToResponse()).ToList());
});

app.MapGet("/api/pizzas/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando pizza {Id}", id);
    return await db.Pizzas.FindAsync(id) is Pizza pizza
        ? Results.Ok(pizza.ToResponse())
        : Results.NotFound(new ErrorResponse("PIZZA-404", $"Pizza con ID {id} no encontrada"));
});

app.MapPost("/api/pizzas", async (CrearPizzaRequest request, PedidoDbContext db, IValidator<CrearPizzaRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("PIZZA-400", "No se pudo crear la pizza. Corregí los datos.", errores));
    }

    var pizza = new Pizza
    {
        Nombre = request.Nombre,
        Descripcion = request.Descripcion,
        Precio = request.Precio,
        Tamano = Enum.Parse<TamanoPizza>(request.Tamano)
    };

    db.Pizzas.Add(pizza);
    await db.SaveChangesAsync();
    logger.LogInformation("Pizza creada: {Id} - {Nombre}", pizza.Id, pizza.Nombre);
    return Results.Created($"/api/pizzas/{pizza.Id}", pizza.ToResponse());
});

app.MapDelete("/api/pizzas/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Eliminando pizza {Id}", id);
    var pizza = await db.Pizzas.FindAsync(id);
    if (pizza is null)
        return Results.NotFound(new ErrorResponse("PIZZA-404", $"Pizza con ID {id} no encontrada"));

    var estaEnPedidos = await db.PedidoPizzas.AnyAsync(pp => pp.PizzaId == id);
    if (estaEnPedidos)
        return Results.BadRequest(new ErrorResponse("PIZZA-409", $"No se puede eliminar la pizza '{pizza.Nombre}' porque está en uno o más pedidos."));

    db.Pizzas.Remove(pizza);
    await db.SaveChangesAsync();
    logger.LogInformation("Pizza eliminada: {Id}", id);
    return Results.NoContent();
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

    var pedidoCreado = await db.Pedidos
        .Include(p => p.Cliente)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstAsync(p => p.Id == pedido.Id);

    logger.LogInformation("Pedido creado: #{Id} - Cliente: {Cliente} - Total: {Total}",
        pedido.Id, cliente.Nombre, pedido.Total);

    await socketServer.NotificarNuevoPedidoAsync(pedido);

    return Results.Created($"/api/pedidos/{pedido.Id}", pedidoCreado.ToResponse());
});

app.MapGet("/api/pedidos/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando pedido #{Id}", id);
    return await db.Pedidos
        .Include(p => p.Cliente)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstOrDefaultAsync(p => p.Id == id) is Pedido pedido
            ? Results.Ok(pedido.ToResponse())
            : Results.NotFound(new ErrorResponse("PEDIDO-404", $"Pedido con ID {id} no encontrado"));
});

app.MapPatch("/api/pedidos/{id}/estado", async (int id, ActualizarEstadoRequest? request, PedidoDbContext db, IValidator<ActualizarEstadoRequest> validator) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Estado))
    {
        return Results.BadRequest(new ErrorResponse("ESTADO-400",
            "Debés enviar un cuerpo JSON con el campo \"estado\". Ejemplo: { \"estado\": \"EnPreparacion\" }"));
    }

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

    var pedidoActualizado = await db.Pedidos
        .Include(p => p.Cliente)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstAsync(p => p.Id == id);

    return Results.Ok(pedidoActualizado.ToResponse());
});

app.MapDelete("/api/pedidos/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Eliminando pedido #{Id}", id);
    var pedido = await db.Pedidos
        .Include(p => p.PedidoPizzas)
        .FirstOrDefaultAsync(p => p.Id == id);
    if (pedido is null)
        return Results.NotFound(new ErrorResponse("PEDIDO-404", $"Pedido con ID {id} no encontrado"));

    db.PedidoPizzas.RemoveRange(pedido.PedidoPizzas);
    db.Pedidos.Remove(pedido);
    await db.SaveChangesAsync();
    logger.LogInformation("Pedido eliminado: #{Id}", id);
    return Results.NoContent();
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
