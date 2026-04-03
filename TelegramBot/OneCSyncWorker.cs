using Application.Interfaces;

namespace TelegramBot;

public class OneCSyncWorker(
    IServiceProvider serviceProvider,
    ILogger<OneCSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Устанавливаем интервал в 1 минуту
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Создаем Scope, так как IOneCService у нас скорее всего Scoped/Transient
                using var scope = serviceProvider.CreateScope();

                var oneCService = scope.ServiceProvider.GetRequiredService<IOneCService>();
                var authCache = scope.ServiceProvider.GetRequiredService<IAuthorizationCacheService>();

                var users = await oneCService.GetAuthorizedTgUsersAsync(stoppingToken);

                authCache.UpdateCache(users);

                logger.LogInformation("Успешная синхронизация с 1С. В кэше {Count} пользователей.", users.Count());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при фоновой синхронизации пользователей с 1С.");
            }
        }
    }
}