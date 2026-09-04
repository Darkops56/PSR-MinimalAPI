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

builder.Services.AddDbContext<PizzeriaDbContext>(options =>
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
              .WithHeaders("Content-Type", "X-Requested-With", "Authorization", "Cookie", "Cache-Control", "Pragma")
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
    PizzeriaDbContext db,
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

        // 4. Crear Dirección de entrega por defecto "Mi casa"
        var direccionPrincipal = new DireccionCliente
        {
            ClienteId = cliente.Id,
            Nombre = "Mi casa",
            DireccionCompleta = cliente.Direccion,
            EsPrincipal = true
        };
        db.DireccionesCliente.Add(direccionPrincipal);

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
    PizzeriaDbContext db,
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

app.MapGet("/api/auth/me/{clienteId}", async (
    int clienteId,
    HttpContext httpContext,
    PizzeriaDbContext db,
    ITokenService tokenService) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != clienteId)
        return Results.Unauthorized();

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

app.MapGet("/api/carrito/{clienteId}", async (
    int clienteId,
    HttpContext httpContext,
    ITokenService tokenService,
    PizzeriaDbContext db) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != clienteId)
        return Results.Unauthorized();

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

app.MapPost("/api/carrito/{clienteId}/items", async (
    int clienteId,
    AgregarCarritoItemRequest request,
    HttpContext httpContext,
    ITokenService tokenService,
    PizzeriaDbContext db) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != clienteId)
        return Results.Unauthorized();

    var pizza = await db.Pizzas.FindAsync(request.PizzaId);
    if (pizza is null)
        return Results.NotFound(new ErrorResponse("PIZZA-404", "Pizza no encontrada en el catálogo."));

    if (!Enum.TryParse<TamanoPizza>(request.Tamano, ignoreCase: true, out var tamano))
        tamano = TamanoPizza.Grande;

    var cantidad = Math.Max(1, request.Cantidad);

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
    var cantidadTotalFormulada = (itemExistente?.Cantidad ?? 0) + cantidad;

    if (cantidadTotalFormulada > pizza.Stock)
    {
        return Results.BadRequest(new ErrorResponse("STOCK-400",
            $"Stock insuficiente para {pizza.Nombre}. Tenés {itemExistente?.Cantidad ?? 0} en el carrito y solo hay {pizza.Stock} unidades en stock."));
    }

    var precioUnitario = CalcularPrecioPorTamano(pizza.Precio, tamano);
    var subtotal = precioUnitario * cantidad;

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

app.MapPut("/api/carrito/items/{itemId}", async (int itemId, ActualizarCarritoItemRequest request, PizzeriaDbContext db) =>
{
    var item = await db.CarritoItems
        .Include(i => i.Carrito)
        .Include(i => i.Pizza)
        .FirstOrDefaultAsync(i => i.Id == itemId);

    if (item is null)
        return Results.NotFound(new ErrorResponse("CARRITO-404", "Item de carrito no encontrado."));

    if (request.Cantidad > item.Pizza.Stock)
    {
        return Results.BadRequest(new ErrorResponse("STOCK-400",
            $"Stock insuficiente para {item.Pizza.Nombre}. El stock máximo disponible es de {item.Pizza.Stock} unidades."));
    }

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

app.MapDelete("/api/carrito/items/{itemId}", async (int itemId, PizzeriaDbContext db) =>
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

app.MapDelete("/api/carrito/{clienteId}/vaciar", async (
    int clienteId,
    HttpContext httpContext,
    ITokenService tokenService,
    PizzeriaDbContext db) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != clienteId)
        return Results.Unauthorized();

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
CheckoutRequest? request,
    HttpContext httpContext,
    ITokenService tokenService,
    PizzeriaDbContext db,
    SocketServerService socketServer) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != clienteId)
        return Results.Unauthorized();

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
        // 0. Validar stock disponible de cada pizza y descontar stock en BD
        foreach (var item in carrito.Items)
        {
            var pizza = await db.Pizzas.FindAsync(item.PizzaId);
            if (pizza is null || pizza.Stock < item.Cantidad)
            {
                await transaction.RollbackAsync();
                var disp = pizza?.Stock ?? 0;
                var nombre = pizza?.Nombre ?? "Pizza";
                return Results.BadRequest(new ErrorResponse("STOCK-400",
                    $"Stock insuficiente para la pizza '{nombre}'. Solicitaste {item.Cantidad} unidades pero solo quedan {disp} en stock."));
            }

            pizza.Stock -= item.Cantidad;
        }

        // Determinar dirección de entrega elegida
        string direccionEntrega = string.Empty;
        if (request?.DireccionId.HasValue == true && request.DireccionId.Value > 0)
        {
            var dirObj = await db.DireccionesCliente.FirstOrDefaultAsync(d => d.Id == request.DireccionId.Value && d.ClienteId == clienteId);
            if (dirObj is not null)
            {
                direccionEntrega = $"{dirObj.Nombre}: {dirObj.DireccionCompleta}";
            }
        }

        if (string.IsNullOrWhiteSpace(direccionEntrega) && !string.IsNullOrWhiteSpace(request?.DireccionEntrega))
        {
            direccionEntrega = request.DireccionEntrega.Trim();
        }

        if (string.IsNullOrWhiteSpace(direccionEntrega))
        {
            var dirPrincipal = await db.DireccionesCliente.FirstOrDefaultAsync(d => d.ClienteId == clienteId && d.EsPrincipal);
            if (dirPrincipal is not null)
            {
                direccionEntrega = $"{dirPrincipal.Nombre}: {dirPrincipal.DireccionCompleta}";
            }
            else
            {
                direccionEntrega = cliente.Direccion;
            }
        }

        // 1. Crear nuevo pedido
        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            FechaPedido = DateTime.UtcNow,
            Estado = EstadoPedido.EnPreparacion,
            Total = carrito.Total,
            DireccionEntrega = direccionEntrega,
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

app.MapGet("/api/pizzas", async (PizzeriaDbContext db) =>
{
    logger.LogInformation("Consultando catálogo de pizzas");
    var pizzas = await db.Pizzas.ToListAsync();
    return Results.Ok(pizzas.Select(p => p.ToResponse()).ToList());
});

app.MapGet("/api/pizzas/{id}", async (int id, PizzeriaDbContext db) =>
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
    PizzeriaDbContext db,
    ITokenService tokenService) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value <= 0)
        return Results.Unauthorized();

    var targetClienteId = tokenClienteId.Value;

    var pedidoActivo = await db.Pedidos
        .AsNoTracking()
        .Include(p => p.Cliente)
            .ThenInclude(c => c.Usuario)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .Where(p => p.ClienteId == targetClienteId && (p.Estado == EstadoPedido.EnPreparacion || p.Estado == EstadoPedido.EnViaje))
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
            .Where(p => p.ClienteId == targetClienteId)
            .OrderByDescending(p => p.FechaPedido)
            .FirstOrDefaultAsync();

        if (ultimo is null) return Results.NotFound();
        httpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        return Results.Ok(ultimo.ToResponse());
    }

    httpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    return Results.Ok(pedidoActivo.ToResponse());
});

app.MapGet("/api/pedidos/mis-pedidos-activos", async (
    HttpContext httpContext,
    PizzeriaDbContext db,
    ITokenService tokenService) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value <= 0)
        return Results.Unauthorized();

    var targetClienteId = tokenClienteId.Value;

    var limiteTiempoLocal = DateTime.UtcNow.AddMinutes(-60);
    var pedidosActivos = await db.Pedidos
        .AsNoTracking()
        .Include(p => p.Cliente)
            .ThenInclude(c => c.Usuario)
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .Where(p => p.ClienteId == targetClienteId && 
                   (p.Estado == EstadoPedido.EnPreparacion || 
                    p.Estado == EstadoPedido.EnViaje || 
                    (p.Estado == EstadoPedido.Entregado && p.FechaPedido >= limiteTiempoLocal)))
        .OrderByDescending(p => p.FechaPedido)
        .ToListAsync();

    httpContext.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    return Results.Ok(pedidosActivos.Select(p => p.ToResponse()).ToList());
});

app.MapGet("/api/pedidos/cliente/{clienteId}", async (
    int clienteId,
    HttpContext httpContext,
    PizzeriaDbContext db,
    ITokenService tokenService) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != clienteId)
    {
        return Results.Unauthorized();
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

app.MapGet("/api/pedidos/{id}", async (int id, PizzeriaDbContext db) =>
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

// 🔁 REPETIR PEDIDO (con validación de stock)
app.MapPost("/api/pedidos/{id}/repetir", async (int id, int clienteId, PizzeriaDbContext db) =>
{
    var pedido = await db.Pedidos
        .Include(p => p.PedidoPizzas)
            .ThenInclude(pp => pp.Pizza)
        .FirstOrDefaultAsync(p => p.Id == id && p.ClienteId == clienteId);

    if (pedido is null)
        return Results.NotFound(new ErrorResponse("PEDIDO-404", "Pedido no encontrado o no pertenece a este cliente."));

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

    var itemsSinStockSuficiente = new List<PizzaSinStockItem>();

    foreach (var pp in pedido.PedidoPizzas)
    {
        var pizza = await db.Pizzas.FindAsync(pp.PizzaId);
        if (pizza is null) continue;

        var tamano = pp.Tamano;
        var cantidadSolicitada = pp.Cantidad;

        var itemExistente = carrito.Items.FirstOrDefault(i => i.PizzaId == pp.PizzaId && i.Tamano == tamano);
        var cantidadEnCarrito = itemExistente?.Cantidad ?? 0;

        var stockDisponibleNeto = Math.Max(0, pizza.Stock - cantidadEnCarrito);

        if (stockDisponibleNeto >= cantidadSolicitada)
        {
            var precioUnitario = CalcularPrecioPorTamano(pizza.Precio, tamano);
            if (itemExistente is not null)
            {
                itemExistente.Cantidad += cantidadSolicitada;
                itemExistente.Subtotal = itemExistente.Cantidad * itemExistente.PrecioUnitario;
            }
            else
            {
                var nuevoItem = new CarritoItem
                {
                    CarritoId = carrito.Id,
                    PizzaId = pizza.Id,
                    Tamano = tamano,
                    Cantidad = cantidadSolicitada,
                    PrecioUnitario = precioUnitario,
                    Subtotal = precioUnitario * cantidadSolicitada
                };
                db.CarritoItems.Add(nuevoItem);
            }
        }
        else
        {
            var cantidadAAgregar = stockDisponibleNeto;
            if (cantidadAAgregar > 0)
            {
                var precioUnitario = CalcularPrecioPorTamano(pizza.Precio, tamano);
                if (itemExistente is not null)
                {
                    itemExistente.Cantidad += cantidadAAgregar;
                    itemExistente.Subtotal = itemExistente.Cantidad * itemExistente.PrecioUnitario;
                }
                else
                {
                    var nuevoItem = new CarritoItem
                    {
                        CarritoId = carrito.Id,
                        PizzaId = pizza.Id,
                        Tamano = tamano,
                        Cantidad = cantidadAAgregar,
                        PrecioUnitario = precioUnitario,
                        Subtotal = precioUnitario * cantidadAAgregar
                    };
                    db.CarritoItems.Add(nuevoItem);
                }
            }

            itemsSinStockSuficiente.Add(new PizzaSinStockItem(
                pizza.Id,
                pizza.Nombre,
                tamano.ToString(),
                cantidadSolicitada,
                pizza.Stock,
                cantidadAAgregar
            ));
        }
    }

    carrito.FechaActualizacion = DateTime.UtcNow;
    carrito.Total = carrito.Items.Sum(i => i.Subtotal);
    await db.SaveChangesAsync();

    var carritoActualizado = await db.Carritos
        .Include(c => c.Items)
            .ThenInclude(i => i.Pizza)
        .FirstAsync(c => c.Id == carrito.Id);

    bool totalmenteAgregado = !itemsSinStockSuficiente.Any();
    string mensaje = totalmenteAgregado
        ? "¡Se agregaron todas las pizzas de tu pedido anterior al carrito!"
        : "Algunas pizzas de tu pedido anterior no tenían suficiente stock. Se agregaron las unidades disponibles.";

    var response = new RepetirPedidoResponse(
        totalmenteAgregado,
        mensaje,
        itemsSinStockSuficiente,
        carritoActualizado.ToResponse()
    );

    return Results.Ok(response);
});

// 🏡 GESTIÓN DE DIRECCIONES DE CLIENTE
app.MapGet("/api/clientes/{clienteId}/direcciones", async (int clienteId, PizzeriaDbContext db) =>
{
    var cliente = await db.Clientes.Include(c => c.Direcciones).FirstOrDefaultAsync(c => c.Id == clienteId);
    if (cliente is null)
        return Results.NotFound(new ErrorResponse("CLIENTE-404", "Cliente no encontrado."));

    if (!cliente.Direcciones.Any() && !string.IsNullOrWhiteSpace(cliente.Direccion))
    {
        var dirDefecto = new DireccionCliente
        {
            ClienteId = cliente.Id,
            Nombre = "Mi casa",
            DireccionCompleta = cliente.Direccion,
            EsPrincipal = true
        };
        db.DireccionesCliente.Add(dirDefecto);
        await db.SaveChangesAsync();
        cliente.Direcciones.Add(dirDefecto);
    }

    return Results.Ok(cliente.Direcciones.OrderByDescending(d => d.EsPrincipal).ThenBy(d => d.Id).Select(d => d.ToResponse()).ToList());
});

app.MapPost("/api/clientes/{clienteId}/direcciones", async (int clienteId, CrearDireccionRequest request, PizzeriaDbContext db) =>
{
    var cliente = await db.Clientes.Include(c => c.Direcciones).FirstOrDefaultAsync(c => c.Id == clienteId);
    if (cliente is null)
        return Results.NotFound(new ErrorResponse("CLIENTE-404", "Cliente no encontrado."));

    if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.DireccionCompleta))
        return Results.BadRequest(new ErrorResponse("DIRECCION-400", "Nombre y Dirección completa son obligatorios."));

    if (request.EsPrincipal || !cliente.Direcciones.Any())
    {
        foreach (var d in cliente.Direcciones)
        {
            d.EsPrincipal = false;
        }
    }

    var nuevaDireccion = new DireccionCliente
    {
        ClienteId = clienteId,
        Nombre = request.Nombre.Trim(),
        DireccionCompleta = request.DireccionCompleta.Trim(),
        Notas = request.Notas?.Trim(),
        EsPrincipal = request.EsPrincipal || !cliente.Direcciones.Any()
    };

    db.DireccionesCliente.Add(nuevaDireccion);
    await db.SaveChangesAsync();

    return Results.Created($"/api/clientes/{clienteId}/direcciones/{nuevaDireccion.Id}", nuevaDireccion.ToResponse());
});

app.MapPut("/api/clientes/{clienteId}/direcciones/{id}", async (int clienteId, int id, ActualizarDireccionRequest request, PizzeriaDbContext db) =>
{
    var direccion = await db.DireccionesCliente.FirstOrDefaultAsync(d => d.Id == id && d.ClienteId == clienteId);
    if (direccion is null)
        return Results.NotFound(new ErrorResponse("DIRECCION-404", "Dirección no encontrada."));

    if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.DireccionCompleta))
        return Results.BadRequest(new ErrorResponse("DIRECCION-400", "Nombre y Dirección completa son obligatorios."));

    if (request.EsPrincipal && !direccion.EsPrincipal)
    {
        var otras = await db.DireccionesCliente.Where(d => d.ClienteId == clienteId && d.Id != id).ToListAsync();
        foreach (var o in otras) o.EsPrincipal = false;
    }

    direccion.Nombre = request.Nombre.Trim();
    direccion.DireccionCompleta = request.DireccionCompleta.Trim();
    direccion.Notas = request.Notas?.Trim();
    direccion.EsPrincipal = request.EsPrincipal;

    await db.SaveChangesAsync();
    return Results.Ok(direccion.ToResponse());
});

app.MapDelete("/api/clientes/{clienteId}/direcciones/{id}", async (int clienteId, int id, PizzeriaDbContext db) =>
{
    var direccion = await db.DireccionesCliente.FirstOrDefaultAsync(d => d.Id == id && d.ClienteId == clienteId);
    if (direccion is null)
        return Results.NotFound(new ErrorResponse("DIRECCION-404", "Dirección no encontrada."));

    db.DireccionesCliente.Remove(direccion);
    await db.SaveChangesAsync();

    if (direccion.EsPrincipal)
    {
        var primera = await db.DireccionesCliente.FirstOrDefaultAsync(d => d.ClienteId == clienteId);
        if (primera is not null)
        {
            primera.EsPrincipal = true;
            await db.SaveChangesAsync();
        }
    }

    return Results.Ok(new { mensaje = "Dirección eliminada correctamente" });
});

app.MapPut("/api/clientes/{clienteId}/direcciones/{id}/principal", async (int clienteId, int id, PizzeriaDbContext db) =>
{
    var direcciones = await db.DireccionesCliente.Where(d => d.ClienteId == clienteId).ToListAsync();
    var objetivo = direcciones.FirstOrDefault(d => d.Id == id);
    if (objetivo is null)
        return Results.NotFound(new ErrorResponse("DIRECCION-404", "Dirección no encontrada."));

    foreach (var d in direcciones)
    {
        d.EsPrincipal = (d.Id == id);
    }

    await db.SaveChangesAsync();
    return Results.Ok(objetivo.ToResponse());
});

app.MapPatch("/api/pedidos/{id}/estado", async (int id, PizzeriaDbContext db, SocketServerService socketServer) =>
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

app.MapGet("/api/clientes/{id}", async (int id, PizzeriaDbContext db) =>
{
    logger.LogInformation("Consultando cliente {Id}", id);
    return await db.Clientes
        .Include(c => c.Usuario)
        .FirstOrDefaultAsync(c => c.Id == id) is Cliente cliente
        ? Results.Ok(cliente.ToResponse())
        : Results.NotFound(new ErrorResponse("CLIENTE-404", $"Cliente con ID {id} no encontrado"));
});

app.MapPut("/api/clientes/{id}", async (
    int id,
    ActualizarClienteRequest request,
    HttpContext httpContext,
    ITokenService tokenService,
    IValidator<ActualizarClienteRequest> validator,
    PizzeriaDbContext db) =>
{
    var authHeader = httpContext.Request.Headers.Authorization.ToString();
    var (esValido, tokenClienteId, _) = tokenService.ValidarToken(authHeader);

    if (!esValido || !tokenClienteId.HasValue || tokenClienteId.Value != id)
        return Results.Unauthorized();

    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        var errores = validationResult.Errors
            .Select(e => new ErrorDetalle(e.PropertyName, e.ErrorMessage))
            .ToList();
        return Results.BadRequest(new ErrorResponse("CLIENTE-400", "Datos de actualización inválidos.", errores));
    }

    logger.LogInformation("Actualizando cliente {Id}", id);

    var cliente = await db.Clientes
        .Include(c => c.Usuario)
        .FirstOrDefaultAsync(c => c.Id == id);

    if (cliente is null)
        return Results.NotFound(new ErrorResponse("CLIENTE-404", $"Cliente con ID {id} no encontrado"));

    if (!string.Equals(cliente.Email, request.Email, StringComparison.OrdinalIgnoreCase))
    {
        var emailExiste = await db.Clientes.AnyAsync(c => c.Id != id && c.Email.ToLower() == request.Email.ToLower());
        if (emailExiste)
            return Results.BadRequest(new ErrorResponse("CLIENTE-400", "El correo electrónico ya está registrado por otro usuario."));
    }

    if (cliente.Usuario is not null && !string.Equals(cliente.Usuario.Username, request.Username, StringComparison.OrdinalIgnoreCase))
    {
        var usernameExiste = await db.Usuarios.AnyAsync(u => u.ClienteId != id && u.Username.ToLower() == request.Username.ToLower());
        if (usernameExiste)
            return Results.BadRequest(new ErrorResponse("CLIENTE-400", "El nombre de usuario ya está registrado por otra cuenta."));

        cliente.Usuario.Username = request.Username.Trim();
    }

    cliente.Nombre = request.Nombre.Trim();
    cliente.Direccion = request.Direccion.Trim();
    cliente.Telefono = request.Telefono.Trim();
    cliente.Email = request.Email.Trim();

    await db.SaveChangesAsync();

    logger.LogInformation("Cliente {Id} actualizado con éxito: {Nombre}", id, cliente.Nombre);
    return Results.Ok(cliente.ToResponse());
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
