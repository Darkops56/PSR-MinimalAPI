using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BearPizzeria.Api.Data;
using BearPizzeria.Api.Models;

namespace BearPizzeria.Common.Testing;

public static class TestDatabaseFactory
{
    public static (PizzeriaDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PizzeriaDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new PizzeriaDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }

    public static async Task<(Cliente Cliente, Usuario Usuario, Carrito Carrito)> SeedClienteAsync(
        PizzeriaDbContext context,
        string username = "testuser",
        string email = "testuser@correo.com",
        string passwordHash = "hashed_pass_123",
        string nombre = "Juan Perez",
        string direccion = "Av. Corrientes 1234",
        string telefono = "+54 11 4444-5555")
    {
        var cliente = new Cliente
        {
            Nombre = nombre,
            Direccion = direccion,
            Telefono = telefono,
            Email = email
        };
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        var usuario = new Usuario
        {
            ClienteId = cliente.Id,
            Username = username,
            PasswordHash = passwordHash,
            FechaCreacion = DateTime.UtcNow
        };
        context.Usuarios.Add(usuario);

        var carrito = new Carrito
        {
            ClienteId = cliente.Id,
            FechaActualizacion = DateTime.UtcNow,
            Total = 0.00m
        };
        context.Carritos.Add(carrito);

        var direccionPrincipal = new DireccionCliente
        {
            ClienteId = cliente.Id,
            Nombre = "Casa",
            DireccionCompleta = direccion,
            EsPrincipal = true
        };
        context.DireccionesCliente.Add(direccionPrincipal);

        await context.SaveChangesAsync();

        return (cliente, usuario, carrito);
    }
}

