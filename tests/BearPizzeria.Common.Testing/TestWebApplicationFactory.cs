using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BearPizzeria.Api.Data;

namespace BearPizzeria.Common.Testing;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _keepAliveConnection;
    private readonly string _dbName = "mem_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "clave_super_secreta_de_testing_con_mas_de_32_caracteres_123456789"
            });
        });

        _keepAliveConnection = new SqliteConnection($"Data Source={_dbName};Mode=Memory;Cache=Shared");
        _keepAliveConnection.Open();

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<PizzeriaDbContext>(options =>
            {
                options.UseSqlite($"Data Source={_dbName};Mode=Memory;Cache=Shared");
            });

            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
            db.Database.EnsureCreated();
        });
    }

    public PizzeriaDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<PizzeriaDbContext>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _keepAliveConnection?.Dispose();
    }
}
