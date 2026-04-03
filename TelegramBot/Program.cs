using System.Net.Http.Headers;
using Application.Interfaces;
using Infrastructure.Services;
using TelegramBot.Commands;
using Telegram.Bot;
using TelegramBot.Interfaces;

namespace TelegramBot;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddHttpClient<IOneCService, OneCService>(client =>
        {
            // Читаем базовый URL
            var baseUrl = builder.Configuration.GetValue<string>("ONEC_BASE_URL");
            client.BaseAddress = new Uri(baseUrl ?? throw new Exception("1C Base URL is missing!"));

            // Добавляем заголовок авторизации сразу для всех запросов этого клиента
            var authHeader = builder.Configuration.GetValue<string>("ONEC_AUTH_HEADER");
            client.DefaultRequestHeaders.Add("Authorization", authHeader);

            // Указываем, что всегда ждем JSON
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });


        builder.Services.AddSingleton<ITelegramBotClient>(_ =>
        {
            var token = builder.Configuration.GetValue<string>("TG_BOT_TOKEN");
            return string.IsNullOrEmpty(token) ? throw new Exception("Telegram Token is missing!") : new TelegramBotClient(token);
        });

        builder.Services.AddSingleton<IUserStateService, UserStateService>();
        builder.Services.AddSingleton<IAuthorizationCacheService, AuthorizationCacheService>();

        builder.Services.AddTransient<IBotCommand, StartCommand>();
        builder.Services.AddTransient<IBotCommand, LeftoversCommand>();
        builder.Services.AddTransient<IBotCommand, ClaimCommand>();
        builder.Services.AddTransient<IBotCommand, HelpCommand>();

        builder.Services.AddHostedService<TgBotWorker>();
        builder.Services.AddHostedService<OneCSyncWorker>();

        var worker = builder.Build();

        using (var scope = worker.Services.CreateScope())
        {
            var oneCService = scope.ServiceProvider.GetRequiredService<IOneCService>();
            var authCache = scope.ServiceProvider.GetRequiredService<IAuthorizationCacheService>();

            authCache.UpdateCache(await oneCService.GetAuthorizedTgUsersAsync(CancellationToken.None));
        }

        await worker.RunAsync();
    }
}