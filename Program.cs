using Microsoft.EntityFrameworkCore;
using UrlShortener.Infrastructure.Persistence;

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

var app = builder.Build();

app.Run();
