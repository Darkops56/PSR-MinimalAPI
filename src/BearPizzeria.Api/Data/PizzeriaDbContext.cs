using Microsoft.EntityFrameworkCore;
using BearPizzeria.Api.Models;

namespace BearPizzeria.Api.Data;

public class PizzeriaDbContext(DbContextOptions<PizzeriaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Pizza> Pizzas => Set<Pizza>();
    public DbSet<Carrito> Carritos => Set<Carrito>();
    public DbSet<CarritoItem> CarritoItems => Set<CarritoItem>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<PedidoPizza> PedidoPizzas => Set<PedidoPizza>();
    public DbSet<DireccionCliente> DireccionesCliente => Set<DireccionCliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DireccionCliente>(entity =>
        {
            entity.ToTable("DireccionesCliente");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DireccionCompleta).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Notas).HasMaxLength(200);

            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Direcciones)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Direccion).HasMaxLength(200).HasDefaultValue("");
            entity.Property(e => e.Telefono).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Usuario)
                  .WithOne(u => u.Cliente)
                  .HasForeignKey<Usuario>(u => u.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Carrito)
                  .WithOne(c => c.Cliente)
                  .HasForeignKey<Carrito>(c => c.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Pizza>(entity =>
        {
            entity.ToTable("Pizzas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Descripcion).HasColumnType("TEXT");
            entity.Property(e => e.Precio).HasColumnType("DECIMAL(10,2)").IsRequired();
            entity.Property(e => e.Tamano).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Stock).IsRequired().HasDefaultValue(10);
        });

        modelBuilder.Entity<Carrito>(entity =>
        {
            entity.ToTable("Carritos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FechaActualizacion).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Total).HasColumnType("DECIMAL(10,2)").HasDefaultValue(0.00m);

            entity.HasMany(e => e.Items)
                  .WithOne(i => i.Carrito)
                  .HasForeignKey(i => i.CarritoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CarritoItem>(entity =>
        {
            entity.ToTable("CarritoItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tamano).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Cantidad).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.PrecioUnitario).HasColumnType("DECIMAL(10,2)").IsRequired();
            entity.Property(e => e.Subtotal).HasColumnType("DECIMAL(10,2)").IsRequired();

            entity.HasOne(e => e.Pizza)
                  .WithMany()
                  .HasForeignKey(e => e.PizzaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.ToTable("Pedidos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FechaPedido).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Estado).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Total).HasColumnType("DECIMAL(10,2)");
            entity.Property(e => e.DireccionEntrega).HasMaxLength(200);

            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Pedidos)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PedidoPizza>(entity =>
        {
            entity.ToTable("PedidoPizzas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tamano).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Cantidad).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.PrecioUnitario).HasColumnType("DECIMAL(10,2)");
            entity.Property(e => e.Subtotal).HasColumnType("DECIMAL(10,2)");

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
            new Pizza { Id = 1, Nombre = "Muzzarella", Descripcion = "Muzzarella artesanal, aceitunas verdes seleccionadas y orégano fresco", Precio = 4500.00m, Tamano = TamanoPizza.Grande, Stock = 12 },
            new Pizza { Id = 2, Nombre = "Napolitana", Descripcion = "Muzzarella, rodajas de tomate natural, ajo picado y aceitunas negras", Precio = 5000.00m, Tamano = TamanoPizza.Grande, Stock = 4 },
            new Pizza { Id = 3, Nombre = "Fugazzeta", Descripcion = "Abundante muzzarella, cebolla caramelizada crujiente y orégano", Precio = 4800.00m, Tamano = TamanoPizza.Grande, Stock = 10 },
            new Pizza { Id = 4, Nombre = "Especial", Descripcion = "Muzzarella, jamón cocido premium, morrón asado y aceitunas", Precio = 5500.00m, Tamano = TamanoPizza.Grande, Stock = 10 },
            new Pizza { Id = 5, Nombre = "Calabresa", Descripcion = "Muzzarella, longaniza calabresa picante y toque de ají molido", Precio = 5200.00m, Tamano = TamanoPizza.Grande, Stock = 2 }
        );
    }
}
