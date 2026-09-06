using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MariaDb;
using UrlShortener.Infrastructure.Persistence;
using Xunit;

namespace UrlShortener.Tests.Integration;

public sealed class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MariaDbContainer _dbContainer = new MariaDbBuilder()
        .WithImage("mariadb:11.4")
        .WithDatabase("UrlShortenerTest")
        .WithUsername("root")
        .WithPassword("root")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Явное преобразование типов строки подключения с передачей единого экземпляра MariaDbServerVersion
            // предотвращает Ambiguous Invocation сбои компилятора Roslyn.
            string connectionString = _dbContainer.GetConnectionString();
            var serverVersion = new MariaDbServerVersion(new Version(11, 4));

            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, serverVersion));
        });
    }
}