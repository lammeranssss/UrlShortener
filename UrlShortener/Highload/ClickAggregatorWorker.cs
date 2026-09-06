using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Highload;

public sealed class ClickAggregatorWorker : BackgroundService
{
    private readonly Channel<string> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ClickAggregatorWorker> _logger;

    public ClickAggregatorWorker(
        Channel<string> channel, 
        IServiceProvider serviceProvider, 
        ILogger<ClickAggregatorWorker> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new Dictionary<string, int>(StringComparer.Ordinal);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int readCount = 0;
                // Ограничиваем вычитывание 10,000 кликов за итерацию для предотвращения CPU Starvation.
                while (readCount < 10_000 && _channel.Reader.TryRead(out var shortCode))
                {
                    batch[shortCode] = batch.GetValueOrDefault(shortCode) + 1;
                    readCount++;
                }

                if (batch.Count > 0)
                {
                    await FlushBatchToDatabaseAsync(batch, stoppingToken);
                    batch.Clear();
                }

                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении пакетного сброса кликов в БД.");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task FlushBatchToDatabaseAsync(Dictionary<string, int> batch, CancellationToken ct)
    {
        // Выполняем обновления последовательно в одной сессии DbContext.
        // Так как batch содержит дедуплицированные ключи (shortCode -> sum),
        // row locks в InnoDB и блокировки базы данных полностью исключены.
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var (shortCode, count) in batch)
        {
            await db.Urls
                .Where(u => u.ShortCode == shortCode)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.ClickCount, u => u.ClickCount + count)
                    .SetProperty(u => u.LastAccessedAt, DateTime.UtcNow), ct);
        }
    }
}
