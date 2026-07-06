using System.Net.Http.Json;
using System.Text.Json;

const string BaseUrl = "http://localhost:5250";

using var http = new HttpClient { BaseAddress = new Uri(BaseUrl) };

int? clienteId = null;

while (true)
{
    Console.Clear();
    Console.WriteLine("============================================");
    Console.WriteLine("        BEARPIZZERIA - CLIENTE");
    Console.WriteLine("============================================");
    Console.WriteLine();

    if (clienteId.HasValue)
        Console.WriteLine($"Cliente ID: {clienteId.Value}");
    else
        Console.WriteLine("(sin registrar)");

    Console.WriteLine();
    Console.WriteLine("1 - Registrar cliente");
    Console.WriteLine("2 - Ver catálogo de pizzas");
    Console.WriteLine("3 - Realizar pedido");
    Console.WriteLine("4 - Consultar estado de pedido");
    Console.WriteLine("5 - Salir");
    Console.WriteLine();
    Console.Write("Seleccioná una opción: ");

    var opcion = Console.ReadLine();

    try
    {
        switch (opcion)
        {
            case "1":
                await RegistrarCliente();
                break;
            case "2":
                await VerCatalogo();
                break;
            case "3":
                await RealizarPedido();
                break;
            case "4":
                await ConsultarPedido();
                break;
            case "5":
                Console.WriteLine("\nGracias por usar BearPizzeria!");
                return;
            default:
                Console.WriteLine("Opción inválida");
                break;
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"\nError de conexión: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError: {ex.Message}");
    }

    Console.WriteLine("\nPresioná Enter para continuar...");
    Console.ReadLine();
}

async Task RegistrarCliente()
{
    Console.Write("\nNombre: ");
    var nombre = Console.ReadLine() ?? "";
    Console.Write("Dirección: ");
    var direccion = Console.ReadLine() ?? "";
    Console.Write("Teléfono: ");
    var telefono = Console.ReadLine() ?? "";
    Console.Write("Email: ");
    var email = Console.ReadLine() ?? "";

    var response = await http.PostAsJsonAsync("/api/clientes", new
    {
        Nombre = nombre,
        Direccion = direccion,
        Telefono = telefono,
        Email = email
    });

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Console.WriteLine($"\n{error.GetProperty("mensaje").GetString()}");

        if (error.TryGetProperty("errores", out var errores))
        {
            foreach (var detalle in errores.EnumerateArray())
            {
                Console.WriteLine($"  - {detalle.GetProperty("campo").GetString()}: {detalle.GetProperty("mensaje").GetString()}");
            }
        }
        return;
    }

    var cliente = await response.Content.ReadFromJsonAsync<JsonElement>();
    clienteId = cliente.GetProperty("id").GetInt32();
    Console.WriteLine($"\nCliente registrado con ID: {clienteId}");
}

async Task VerCatalogo()
{
    var pizzas = await http.GetFromJsonAsync<JsonElement>("/api/pizzas");

    Console.WriteLine("\n--- CATÁLOGO DE PIZZAS ---");

    foreach (var pizza in pizzas.EnumerateArray())
    {
        Console.WriteLine($"#{pizza.GetProperty("id").GetInt32()} - {pizza.GetProperty("nombre").GetString()}");
        Console.WriteLine($"   {pizza.GetProperty("descripcion").GetString()}");
        Console.WriteLine($"   ${pizza.GetProperty("precio").GetDecimal()} - {pizza.GetProperty("tamano").GetString()}");
        Console.WriteLine();
    }
}

async Task RealizarPedido()
{
    if (!clienteId.HasValue)
    {
        Console.WriteLine("\nPrimero registrate como cliente (opción 1)");
        return;
    }

    await VerCatalogo();

    Console.WriteLine("Ingresá los IDs de las pizzas (separados por coma):");
    var idsInput = Console.ReadLine() ?? "";
    var pizzaIds = idsInput.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                           .Select(int.Parse).ToList();

    var items = pizzaIds.Select(id => new { PizzaId = id, Cantidad = 1 }).ToList();

    Console.WriteLine("\nEnviando pedido...");
    var response = await http.PostAsJsonAsync("/api/pedidos", new
    {
        ClienteId = clienteId.Value,
        Items = items
    });

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Console.WriteLine($"\n{error.GetProperty("mensaje").GetString()}");

        if (error.TryGetProperty("errores", out var errores))
        {
            foreach (var detalle in errores.EnumerateArray())
            {
                Console.WriteLine($"  - {detalle.GetProperty("campo").GetString()}: {detalle.GetProperty("mensaje").GetString()}");
            }
        }
        return;
    }

    var pedido = await response.Content.ReadFromJsonAsync<JsonElement>();

    Console.WriteLine($"\nPedido #{pedido.GetProperty("id").GetInt32()} creado!");
    Console.WriteLine($"Estado: {pedido.GetProperty("estado").GetString()}");
    Console.WriteLine($"Total: ${pedido.GetProperty("total").GetDecimal()}");
}

async Task ConsultarPedido()
{
    Console.Write("\nNúmero de pedido: ");
    if (!int.TryParse(Console.ReadLine(), out var pedidoId))
    {
        Console.WriteLine("Número inválido");
        return;
    }

    var response = await http.GetAsync($"/api/pedidos/{pedidoId}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine("Pedido no encontrado");
        return;
    }

    var pedido = await response.Content.ReadFromJsonAsync<JsonElement>();

    Console.WriteLine($"\n--- PEDIDO #{pedidoId} ---");
    Console.WriteLine($"Estado: {pedido.GetProperty("estado").GetString()}");
    Console.WriteLine($"Total: ${pedido.GetProperty("total").GetDecimal()}");
    Console.WriteLine($"Fecha: {pedido.GetProperty("fechaPedido").GetString()}");

    Console.WriteLine("\nItems:");
    foreach (var pp in pedido.GetProperty("pedidoPizzas").EnumerateArray())
    {
        var pizza = pp.GetProperty("pizza");
        Console.WriteLine($"  - {pp.GetProperty("cantidad").GetInt32()}x {pizza.GetProperty("nombre").GetString()} (${pp.GetProperty("precioUnitario").GetDecimal()} c/u)");
    }
}
