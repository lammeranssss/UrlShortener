using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UrlRecord> Urls => Set<UrlRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Автоматически применяет все конфигурации (IEntityTypeConfiguration) из текущей сборки.
        // Это избавляет от необходимости регистрировать каждую таблицу вручную.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}