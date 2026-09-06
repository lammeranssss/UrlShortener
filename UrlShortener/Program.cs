using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using MySqlConnector;
using UrlShortener.Contracts;
using UrlShortener.Domain.Entities;
using UrlShortener.Domain.Services;
using UrlShortener.Highload;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

const string dbConnectionStringName = "DefaultConnection";
var connectionString = builder.Configuration.GetConnectionString(dbConnectionStringName);

// Паттерн Fail-Fast. Приложение не должно запускаться с невалидной конфигурацией.
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException($"Connection string '{dbConnectionStringName}' is missing or empty.");
}

// Явное указание версии исключает синхронный сетевой I/O запрос при сборке DI.
// Запуск становится детерминированным и не зависит от доступности БД в нулевую секунду.
var serverVersion = new MariaDbServerVersion(new Version(11, 4));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
    {
        // Стандарт для Cloud-Native. 
        // Автоматически повторяет транзакции при кратковременных сетевых сбоях БД.
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024;
});
#pragma warning restore EXTEXP0018

var clickChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(100_000)
{
    FullMode = BoundedChannelFullMode.DropOldest,
    SingleWriter = false,
    SingleReader = true
});
builder.Services.AddSingleton(clickChannel);
builder.Services.AddHostedService<ClickAggregatorWorker>();

builder.Services.AddSingleton<IShortCodeGenerator, CryptographicCodeGenerator>();
builder.Services.AddSingleton<IDnsResolver, SystemDnsResolver>();

builder.Services.AddTransient<UrlValidator>();

const string corsPolicyName = "ReactAppPolicy";
var allowedOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// CLI-режим применения миграций для изолированного migration-runner контейнера.
// Выполняет DDL-изменения до запуска Kestrel, соблюдая требование ТЗ "запуск в 1 клик".
if (args.Contains("--apply-migrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.UseCors(corsPolicyName);

var apiGroup = app.MapGroup("/api/urls");

apiGroup.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
{
    var urls = await db.Urls
        .AsNoTracking()
        .OrderByDescending(x => x.CreatedAt)
        .Take(100)
        .Select(u => new UrlRecordDto(
            u.Id,
            u.OriginalUrl,
            u.ShortCode,
            u.ClickCount,
            u.CreatedAt,
            u.LastAccessedAt))
        .ToListAsync(ct);

    return Results.Ok(urls);
});

apiGroup.MapPost("/", async (
    CreateUrlRequest request,
    UrlValidator validator,
    IShortCodeGenerator generator,
    AppDbContext db,
    CancellationToken ct) =>
{
    var (isValid, errorMessage) = await validator.ValidateAsync(request.OriginalUrl, ct);
    if (!isValid)
    {
        return Results.BadRequest(new { error = errorMessage });
    }

    const int maxRetries = 3;
    for (int retry = 0; retry < maxRetries; retry++)
    {
        var record = new UrlRecord
        {
            OriginalUrl = request.OriginalUrl,
            ShortCode = generator.GenerateCode()
        };

        try
        {
            db.Urls.Add(record);
            await db.SaveChangesAsync(ct);

            var dto = new UrlRecordDto(
                record.Id, 
                record.OriginalUrl, 
                record.ShortCode, 
                record.ClickCount, 
                record.CreatedAt, 
                record.LastAccessedAt);

            return Results.Created($"/api/urls/{record.Id}", dto);
        }
        // Прямая проверка MySqlException(1062) без YAGNI-абстракций.
        // Фильтр исключения гарантирует, что повторная попытка генерации кода
        // выполнится только при коллизии уникального индекса (ER_DUP_ENTRY).
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 } && retry < maxRetries - 1)
        {
            db.Entry(record).State = EntityState.Detached;
        }
    }

    return Results.Problem("Не удалось сгенерировать уникальный код. Попробуйте еще раз.");
});

apiGroup.MapPut("/{id:long}", async (
    long id,
    UpdateUrlRequest request,
    UrlValidator validator,
    AppDbContext db,
    HybridCache cache,
    CancellationToken ct) =>
{
    var (isValid, errorMessage) = await validator.ValidateAsync(request.OriginalUrl, ct);
    if (!isValid)
    {
        return Results.BadRequest(new { error = errorMessage });
    }

    var record = await db.Urls.FindAsync([id], ct);
    if (record is null)
    {
        return Results.NotFound();
    }

    record.OriginalUrl = request.OriginalUrl;

    await cache.RemoveAsync($"url:{record.ShortCode}", ct);
    await db.SaveChangesAsync(ct);
    await cache.RemoveAsync($"url:{record.ShortCode}", ct);

    var dto = new UrlRecordDto(
        record.Id, 
        record.OriginalUrl, 
        record.ShortCode, 
        record.ClickCount, 
        record.CreatedAt, 
        record.LastAccessedAt);

    return Results.Ok(dto);
});

apiGroup.MapDelete("/{id:long}", async (
    long id,
    AppDbContext db,
    HybridCache cache,
    CancellationToken ct) =>
{
    var record = await db.Urls.FindAsync([id], ct);
    if (record is null)
    {
        return Results.NotFound();
    }

    db.Urls.Remove(record);

    await cache.RemoveAsync($"url:{record.ShortCode}", ct);
    await db.SaveChangesAsync(ct);
    await cache.RemoveAsync($"url:{record.ShortCode}", ct);

    return Results.NoContent();
});

app.MapGet("/{shortKey:length(7)}", async (
    string shortKey, 
    HybridCache cache, 
    Channel<string> channel, 
    IServiceProvider serviceProvider,
    CancellationToken ct) =>
{
    string? originalUrl = await cache.GetOrCreateAsync(
        key: $"url:{shortKey}",
        factory: async cancel => 
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            return await dbContext.Urls
                .AsNoTracking()
                .Where(u => u.ShortCode == shortKey)
                .Select(u => u.OriginalUrl)
                .FirstOrDefaultAsync(cancel);
        },
        cancellationToken: ct
    );

    if (string.IsNullOrEmpty(originalUrl))
        return Results.NotFound("Короткая ссылка не найдена.");

    channel.Writer.TryWrite(shortKey);

    return Results.Redirect(originalUrl, permanent: false);
});

app.Run();

public partial class Program { }
