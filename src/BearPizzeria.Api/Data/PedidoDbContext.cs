using Microsoft.EntityFrameworkCore;
using BearPizzeria.Api.Models;

namespace BearPizzeria.Api.Data;

public class PedidoDbContext(DbContextOptions<PedidoDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pizza> Pizzas => Set<Pizza>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoPizza> PedidoPizzas => Set<PedidoPizza>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Direccion).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Telefono).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Usuario).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Usuario).IsUnique();
        });

        modelBuilder.Entity<Pizza>(entity =>
        {
            entity.ToTable("Pizzas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnType("TEXT");
            entity.Property(e => e.Precio).HasColumnType("DECIMAL(10,2)").IsRequired();
            entity.Property(e => e.Tamano).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedidos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FechaPedido).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Estado).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Total).HasColumnType("DECIMAL(10,2)");

            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Pedidos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoPizza>(entity =>
        {
            entity.ToTable("PedidoPizzas");
            entity.HasKey(e => new { e.PedidoId, e.PizzaId });
            entity.Property(e => e.Cantidad).IsRequired();
            entity.Property(e => e.PrecioUnitario).HasColumnType("DECIMAL(10,2)");

            entity.HasOne(e => e.Pedido)
                  .WithMany(p => p.PedidoPizzas)
                  .HasForeignKey(e => e.PedidoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Pizza)
                  .WithMany(p => p.PedidoPizzas)
                  .HasForeignKey(e => e.PizzaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pizza>().HasData(
            new Pizza { Id = 1, Nombre = "Muzzarella", Descripcion = "Muzzarella, aceitunas y orégano", Precio = 4500.00m, Tamano = TamanoPizza.Grande },
            new Pizza { Id = 2, Nombre = "Napolitana", Descripcion = "Muzzarella, tomate, ajo y aceitunas", Precio = 5000.00m, Tamano = TamanoPizza.Grande },
            new Pizza { Id = 3, Nombre = "Fugazzeta", Descripcion = "Muzzarella, cebolla y aceitunas", Precio = 4800.00m, Tamano = TamanoPizza.Grande },
            new Pizza { Id = 4, Nombre = "Especial", Descripcion = "Muzzarella, jamón, morrón y aceitunas", Precio = 5500.00m, Tamano = TamanoPizza.Grande },
            new Pizza { Id = 5, Nombre = "Calabresa", Descripcion = "Muzzarella, longaniza calabresa y aceitunas", Precio = 5200.00m, Tamano = TamanoPizza.Grande }
        );
    }
}
