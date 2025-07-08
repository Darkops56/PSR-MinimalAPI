using System.Data;
using MySqlConnector;
using Scalar.AspNetCore;
using TodoPSR;

var builder = WebApplication.CreateBuilder(args);

//  Obtener la cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("MySQL");

//  Registrando IDbConnection para que se inyecte como dependencia
//  Cada vez que se inyecte, se creará una nueva instancia con la cadena de conexión
builder.Services.AddScoped<IDbConnection>(sp => new MySqlConnection(connectionString));

//Cada vez que necesite la interfaz, se va a instanciar automaticamente AdoDapper y se va a pasar al metodo de la API
builder.Services.AddScoped<IADO, AdoDapper>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/openapi/{documentName}.json";
    });
    app.MapScalarApiReference();
}

//Para un GET en la ruta "/todoitems", 
app.MapGet("/todoitems", async (IADO repo) =>
    await repo.ObtenerTodosAsync());

app.MapGet("/todoitems/complete", async (IADO repo) =>
    await repo.ObtenerTodosFinalizadosAsync());

app.MapGet("/todoitems/{id}", async (int id, IADO repo) =>
    await repo.ObtenerTodoPorIdAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todoitems", async (Todo todo, IADO repo) =>
{
    await repo.AgregarTodoAsync(todo);

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, IADO repo) =>
{
    var todo = await repo.ObtenerTodoPorIdAsync(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    await repo.ActualizarTodoAsync(todo);

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", async (int id, IADO repo) =>
{
    if (await repo.ObtenerTodoPorIdAsync(id) is Todo todo)
    {
        await repo.EliminarTodoAsync(todo);
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();