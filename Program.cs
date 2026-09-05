using Microsoft.EntityFrameworkCore;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Паттерн Fail-Fast. Приложение не должно запускаться с невалидной конфигурацией.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not found in appsettings.json.");
}

// Явное указание версии исключает синхронный сетевой I/O запрос при сборке DI.
// Запуск становится детерминированным и не зависит от доступности БД в нулевую секунду.
var serverVersion = new MariaDbServerVersion(new Version(11, 4));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));

var app = builder.Build();

app.Run();
