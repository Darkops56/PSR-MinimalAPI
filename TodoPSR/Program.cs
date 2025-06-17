using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TodoPSR;

var builder = WebApplication.CreateBuilder(args);

//Carga la Bd en memoria RAM
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

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
app.MapGet("/todoitems", (TodoDb db) =>
    db.Todos.ToList());

app.MapGet("/todoitems/complete", (TodoDb db) =>
    db.Todos.Where(t => t.IsComplete).ToList());

app.MapGet("/todoitems/{id}", (int id, TodoDb db) =>
    db.Todos.Find(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todoitems", (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    db.SaveChanges();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

app.MapPut("/todoitems/{id}", (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = db.Todos.Find(id);

    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;

    db.SaveChanges();

    return Results.NoContent();
});

app.MapDelete("/todoitems/{id}", (int id, TodoDb db) =>
{
    if (db.Todos.Find(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        db.SaveChanges();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();