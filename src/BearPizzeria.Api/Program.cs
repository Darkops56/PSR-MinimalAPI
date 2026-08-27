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

builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<SocketServerService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SocketServerService>());

// Configuración de CORS Estricto: Aislamiento exclusivo con el frontend MVC
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMvcClient", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5082",
                "https://localhost:7188"
              )
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
              .WithHeaders("Content-Type", "X-Requested-With", "Authorization", "Cookie")
              .AllowCredentials();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "BearPizzeria API";
        document.Info.Version = "v1";
        document.Info.Description = "API de gestión de pedidos, carritos y autenticación de pizzería";
        return Task.CompletedTask;
    });
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.UseCors("AllowMvcClient");

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("BearPizzeria API")
           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// ==========================================
// 🔐 AUTENTICACIÓN Y TOKEN SERVICE (JWT)
// ==========================================

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    PedidoDbContext db,
    IPasswordHasherService passwordHasher,
    ITokenService tokenService,
    IValidator<RegisterRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("AUTH-400", "Datos de registro inválidos.", errores));
    }

    var erroresUnicos = new List<ErrorDetalle>();

    if (await db.Usuarios.AnyAsync(u => u.Username == request.Username))
        erroresUnicos.Add(new ErrorDetalle("Username", $"El usuario '{request.Username}' ya está registrado."));

    if (await db.Clientes.AnyAsync(c => c.Email == request.Email))
        erroresUnicos.Add(new ErrorDetalle("Email", $"El email '{request.Email}' ya está registrado."));

    if (erroresUnicos.Count > 0)
        return Results.BadRequest(new ErrorResponse("AUTH-400", "No se pudo completar el registro.", erroresUnicos));

    using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        // 1. Instanciar y guardar Cliente primero
        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Direccion = request.Direccion.Trim(),
            Telefono = request.Telefono.Trim(),
            Email = request.Email.Trim().ToLowerInvariant()
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        // 2. Encriptar contraseña y crear Usuario vinculado a Cliente
        var passwordHash = passwordHasher.HashPassword(request.Password);
        var usuario = new Usuario
        {
            ClienteId = cliente.Id,
            Username = request.Username.Trim(),
            PasswordHash = passwordHash,
            FechaCreacion = DateTime.UtcNow,
            UltimoAcceso = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);

        // 3. Crear Carrito persistido inicial para el cliente
        var carrito = new Carrito
        {
            ClienteId = cliente.Id,
            FechaActualizacion = DateTime.UtcNow,
            Total = 0.00m
        };
        db.Carritos.Add(carrito);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        logger.LogInformation("Cliente y Usuario registrados exitosamente: ID Cliente {ClienteId} - Usuario {Username}", cliente.Id, usuario.Username);

        var token = tokenService.GenerarToken(cliente.Id, usuario.Username, cliente.Email);

        var response = new AuthResponse(
            cliente.Id,
            usuario.Username,
            cliente.Nombre,
            cliente.Email,
            cliente.Direccion,
            cliente.Telefono,
            token
        );

        return Results.Created($"/api/clientes/{cliente.Id}", response);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        logger.LogError(ex, "Error al registrar cliente y usuario");
        return Results.Problem("Ocurrió un error interno al registrar la cuenta.");
    }
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    PedidoDbContext db,
    IPasswordHasherService passwordHasher,
    ITokenService tokenService,
    IValidator<LoginRequest> validator) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("AUTH-400", "Credenciales inválidas.", errores));
    }

    var usuario = await db.Usuarios
        .Include(u => u.Cliente)
        .FirstOrDefaultAsync(u => u.Username == request.Username);

    if (usuario is null || !passwordHasher.VerifyPassword(request.Password, usuario.PasswordHash))
    {
        return Results.BadRequest(new ErrorResponse("AUTH-401", "Usuario o contraseña incorrectos."));
    }

    usuario.UltimoAcceso = DateTime.UtcNow;
    await db.SaveChangesAsync();

    logger.LogInformation("Usuario autenticado: {Username} (Cliente ID {ClienteId})", usuario.Username, usuario.ClienteId);

    var token = tokenService.GenerarToken(usuario.Cliente.Id, usuario.Username, usuario.Cliente.Email);

    var response = new AuthResponse(
        usuario.Cliente.Id,
        usuario.Username,
        usuario.Cliente.Nombre,
        usuario.Cliente.Email,
        usuario.Cliente.Direccion,
        usuario.Cliente.Telefono,
        token
    );

    return Results.Ok(response);
});

app.MapGet("/api/auth/me/{clienteId}", async (int clienteId, PedidoDbContext db, ITokenService tokenService) =>
{
    var usuario = await db.Usuarios
        .Include(u => u.Cliente)
        .FirstOrDefaultAsync(u => u.ClienteId == clienteId);

    if (usuario is null)
        return Results.NotFound(new ErrorResponse("AUTH-404", "Sesión de usuario no encontrada."));

    var token = tokenService.GenerarToken(usuario.Cliente.Id, usuario.Username, usuario.Cliente.Email);

    var response = new AuthResponse(
        usuario.Cliente.Id,
        usuario.Username,
        usuario.Cliente.Nombre,
        usuario.Cliente.Email,
        usuario.Cliente.Direccion,
        usuario.Cliente.Telefono,
        token
    );

    return Results.Ok(response);
});

// ==========================================
// 🛒 CARRITO DE COMPRAS (100% EN BASE DE DATOS)
// ==========================================

app.MapGet("/api/carrito/{clienteId}", async (int clienteId, PedidoDbContext db) =>
{
    var carrito = await db.Carritos
        .Include(c => c.Items)
            .ThenInclude(i => i.Pizza)
        .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

    if (carrito is null)
    {
        var nuevoCarrito = new Carrito
        {
            ClienteId = clienteId,
            FechaActualizacion = DateTime.UtcNow,
            Total = 0.00m
        };
        db.Carritos.Add(nuevoCarrito);
        await db.SaveChangesAsync();
        return Results.Ok(nuevoCarrito.ToResponse());
    }

    return Results.Ok(carrito.ToResponse());
});

app.MapPost("/api/carrito/{clienteId}/items", async (int clienteId, AgregarCarritoItemRequest request, PedidoDbContext db) =>
{
    var pizza = await db.Pizzas.FindAsync(request.PizzaId);
    if (pizza is null)
        return Results.NotFound(new ErrorResponse("PIZZA-404", "Pizza no encontrada en el catálogo."));

    if (!Enum.TryParse<TamanoPizza>(request.Tamano, ignoreCase: true, out var tamano))
        tamano = TamanoPizza.Grande;

    var cantidad = Math.Max(1, request.Cantidad);
    var precioUnitario = CalcularPrecioPorTamano(pizza.Precio, tamano);
    var subtotal = precioUnitario * cantidad;

    var carrito = await db.Carritos
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

    if (carrito is null)
    {
        carrito = new Carrito
        {
            ClienteId = clienteId,
            FechaActualizacion = DateTime.UtcNow,
            Total = 0.00m
        };
        db.Carritos.Add(carrito);
        await db.SaveChangesAsync();
    }

    var itemExistente = carrito.Items.FirstOrDefault(i => i.PizzaId == request.PizzaId && i.Tamano == tamano);
    if (itemExistente is not null)
    {
        itemExistente.Cantidad += cantidad;
        itemExistente.Subtotal = itemExistente.Cantidad * itemExistente.PrecioUnitario;
    }
    else
    {
        var nuevoItem = new CarritoItem
        {
            CarritoId = carrito.Id,
            PizzaId = pizza.Id,
            Tamano = tamano,
            Cantidad = cantidad,
            PrecioUnitario = precioUnitario,
            Subtotal = subtotal
        };
        db.CarritoItems.Add(nuevoItem);
        carrito.Items.Add(nuevoItem);
    }

    carrito.FechaActualizacion = DateTime.UtcNow;
    carrito.Total = carrito.Items.Sum(i => i.Subtotal);

    await db.SaveChangesAsync();

    var carritoActualizado = await db.Carritos
        .Include(c => c.Items)
            .ThenInclude(i => i.Pizza)
        .FirstAsync(c => c.Id == carrito.Id);

    logger.LogInformation("Item añadido al carrito del cliente {ClienteId}: {Pizza} ({Tamano}) x{Cantidad}",
        clienteId, pizza.Nombre, tamano, cantidad);

    return Results.Ok(carritoActualizado.ToResponse());
});

app.MapPut("/api/carrito/items/{itemId}", async (int itemId, ActualizarCarritoItemRequest request, PedidoDbContext db) =>
{
    var item = await db.CarritoItems
        .Include(i => i.Carrito)
        .Include(i => i.Pizza)
        .FirstOrDefaultAsync(i => i.Id == itemId);

    if (item is null)
        return Results.NotFound(new ErrorResponse("CARRITO-404", "Item de carrito no encontrado."));

    if (request.Cantidad <= 0)
    {
        db.CarritoItems.Remove(item);
    }
    else
    {
        item.Cantidad = request.Cantidad;
        item.Subtotal = item.Cantidad * item.PrecioUnitario;
    }

    await db.SaveChangesAsync();

    var carrito = await db.Carritos
        .Include(c => c.Items)
            .ThenInclude(i => i.Pizza)
        .FirstAsync(c => c.Id == item.CarritoId);

    carrito.FechaActualizacion = DateTime.UtcNow;
    carrito.Total = carrito.Items.Sum(i => i.Subtotal);
    await db.SaveChangesAsync();

    return Results.Ok(carrito.ToResponse());
});

app.MapDelete("/api/carrito/items/{itemId}", async (int itemId, PedidoDbContext db) =>
{
    var item = await db.CarritoItems
        .Include(i => i.Carrito)
        .FirstOrDefaultAsync(i => i.Id == itemId);

    if (item is null)
        return Results.NotFound(new ErrorResponse("CARRITO-404", "Item no encontrado."));

    var carritoId = item.CarritoId;
    db.CarritoItems.Remove(item);
    await db.SaveChangesAsync();

    var carrito = await db.Carritos
        .Include(c => c.Items)
            .ThenInclude(i => i.Pizza)
        .FirstAsync(c => c.Id == carritoId);

    carrito.FechaActualizacion = DateTime.UtcNow;
    carrito.Total = carrito.Items.Sum(i => i.Subtotal);
    await db.SaveChangesAsync();

    return Results.Ok(carrito.ToResponse());
});

app.MapDelete("/api/carrito/{clienteId}/vaciar", async (int clienteId, PedidoDbContext db) =>
{
    var carrito = await db.Carritos
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

    if (carrito is null)
        return Results.NotFound(new ErrorResponse("CARRITO-404", "Carrito no encontrado."));

    db.CarritoItems.RemoveRange(carrito.Items);
    carrito.Total = 0.00m;
    carrito.FechaActualizacion = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(carrito.ToResponse());
});

app.MapPost("/api/carrito/{clienteId}/checkout", async (
    int clienteId,
    PedidoDbContext db,
    SocketServerService socketServer) =>
{
    var carrito = await db.Carritos
        .Include(c => c.Items)
            .ThenInclude(i => i.Pizza)
        .Include(c => c.Cliente)
            .ThenInclude(cl => cl.Usuario)
        .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

    if (carrito is null || carrito.Items.Count == 0)
        return Results.BadRequest(new ErrorResponse("CHECKOUT-400", "El carrito está vacío. Agregá pizzas antes de confirmar."));

    var cliente = carrito.Cliente;

    using var transaction = await db.Database.BeginTransactionAsync();
    try
    {
        // 1. Crear nuevo pedido
        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            FechaPedido = DateTime.UtcNow,
            Estado = EstadoPedido.EnPreparacion,
            Total = carrito.Total,
            PedidoPizzas = carrito.Items.Select(item => new PedidoPizza
            {
                PizzaId = item.PizzaId,
                Tamano = item.Tamano,
                Cantidad = item.Cantidad,
                PrecioUnitario = item.PrecioUnitario,
                Subtotal = item.Subtotal
            }).ToList()
        };

        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();

        // 2. Vaciar carrito en base de datos
        db.CarritoItems.RemoveRange(carrito.Items);
        carrito.Total = 0.00m;
        carrito.FechaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        var pedidoCompleto = await db.Pedidos
            .Include(p => p.Cliente)
                .ThenInclude(c => c.Usuario)
            .Include(p => p.PedidoPizzas)
                .ThenInclude(pp => pp.Pizza)
            .FirstAsync(p => p.Id == pedido.Id);

        logger.LogInformation("Checkout exitoso: Pedido #{PedidoId} para Cliente {Cliente} (Total: ${Total})",
            pedido.Id, cliente.Nombre, pedido.Total);

        // 3. Notificar a Cocina por socket TCP
        await socketServer.NotificarNuevoPedidoAsync(pedidoCompleto);

        return Results.Created($"/api/pedidos/{pedido.Id}", pedidoCompleto.ToResponse());
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        logger.LogError(ex, "Error al realizar checkout para cliente {ClienteId}", clienteId);
        return Results.Problem("Ocurrió un error al procesar el pedido.");
    }
});

// ==========================================
// 🍕 CATÁLOGO DE PIZZAS
// ==========================================

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

// ==========================================
// 📦 PEDIDOS & TRACKING CON VALIDACIÓN DE TOKEN
// ==========================================

app.MapGet("/api/pedidos/mi-pedido-activo", async (
    HttpContext httpContext,
    PedidoDbContext db,
    ITokenService tokenService) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, clienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !clienteId.HasValue)
        return Results.Unauthorized();

    var pedidoActivo = await db.Pedidos
        .AsNoTracking()
        .Include(p => p.Cliente)
            .ThenInclude(c => c.Usuario)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .Where(p => p.ClienteId == clienteId.Value && (p.Estado == EstadoPedido.EnPreparacion || p.Estado == EstadoPedido.EnViaje))
        .OrderByDescending(p => p.FechaPedido)
        .FirstOrDefaultAsync();

    if (pedidoActivo is null)
    {
        // Si no hay en preparacion o en viaje, devolver el más reciente
        var ultimo = await db.Pedidos
            .AsNoTracking()
            .Include(p => p.Cliente)
                .ThenInclude(c => c.Usuario)
            .Include(p => p.PedidoPizzas)
                .ThenInclude(pp => pp.Pizza)
            .Where(p => p.ClienteId == clienteId.Value)
            .OrderByDescending(p => p.FechaPedido)
            .FirstOrDefaultAsync();

        return ultimo is not null ? Results.Ok(ultimo.ToResponse()) : Results.NotFound();
    }

    return Results.Ok(pedidoActivo.ToResponse());
});

app.MapGet("/api/pedidos/cliente/{clienteId}", async (
    int clienteId,
    HttpContext httpContext,
    PedidoDbContext db,
    ITokenService tokenService) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authHeader))
    {
        var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);
        if (esValido && tokenClienteId.HasValue && tokenClienteId.Value != clienteId)
        {
            return Results.Forbid();
        }
    }

    logger.LogInformation("Consultando historial de pedidos para Cliente ID {ClienteId}", clienteId);
    var pedidos = await db.Pedidos
        .AsNoTracking()
        .Include(p => p.Cliente)
            .ThenInclude(c => c.Usuario)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .Where(p => p.ClienteId == clienteId)
        .OrderByDescending(p => p.FechaPedido)
        .ToListAsync();

    return Results.Ok(pedidos.Select(p => p.ToResponse()).ToList());
});

app.MapGet("/api/pedidos/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando pedido #{Id}", id);
    return await db.Pedidos
        .AsNoTracking()
        .Include(p => p.Cliente)
            .ThenInclude(c => c.Usuario)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstOrDefaultAsync(p => p.Id == id) is Pedido pedido
            ? Results.Ok(pedido.ToResponse())
            : Results.NotFound(new ErrorResponse("PEDIDO-404", $"Pedido con ID {id} no encontrado"));
});

app.MapPatch("/api/pedidos/{id}/estado", async (int id, PedidoDbContext db, SocketServerService socketServer) =>
{
    var pedido = await db.Pedidos.FindAsync(id);
    if (pedido is null)
        return Results.NotFound(new ErrorResponse("PEDIDO-404", $"Pedido con ID {id} no encontrado"));

    var siguienteEstado = pedido.Estado switch
    {
        EstadoPedido.EnPreparacion => EstadoPedido.EnViaje,
        EstadoPedido.EnViaje => EstadoPedido.Entregado,
        _ => (EstadoPedido?)null
    };

    if (siguienteEstado is null)
        return Results.BadRequest(new ErrorResponse("ESTADO-400",
            $"El pedido #{id} ya fue entregado. No se puede avanzar al siguiente estado."));

    logger.LogInformation("Pedido #{Id}: {EstadoAnterior} -> {EstadoNuevo}", id, pedido.Estado, siguienteEstado);
    pedido.Estado = siguienteEstado.Value;
    await db.SaveChangesAsync();

    // Si avanzó a EnViaje, notificar al módulo de Reparto por sockets TCP
    if (pedido.Estado == EstadoPedido.EnViaje)
    {
        await socketServer.NotificarPedidoEnViajeAsync(pedido.Id);
    }

    var pedidoActualizado = await db.Pedidos
        .Include(p => p.Cliente)
            .ThenInclude(c => c.Usuario)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstAsync(p => p.Id == id);

    return Results.Ok(pedidoActualizado.ToResponse());
});

// ==========================================
// 👥 CLIENTES
// ==========================================

app.MapGet("/api/clientes/{id}", async (int id, PedidoDbContext db) =>
{
    logger.LogInformation("Consultando cliente {Id}", id);
    return await db.Clientes
        .Include(c => c.Usuario)
        .FirstOrDefaultAsync(c => c.Id == id) is Cliente cliente
        ? Results.Ok(cliente.ToResponse())
        : Results.NotFound(new ErrorResponse("CLIENTE-404", $"Cliente con ID {id} no encontrado"));
});

app.Run();

// ==========================================
// 🛠️ MÉTODOS AUXILIARES Y MULTIPLICADORES
// ==========================================

static decimal CalcularPrecioPorTamano(decimal precioBase, TamanoPizza tamano)
{
    var multiplicador = tamano switch
    {
        TamanoPizza.Personal => 0.70m,
        TamanoPizza.Mediana => 0.85m,
        TamanoPizza.Grande => 1.00m,
        TamanoPizza.Familiar => 1.30m,
        _ => 1.00m
    };

    return Math.Round(precioBase * multiplicador, 2);
}

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
