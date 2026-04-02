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


        // var host = builder.Configuration.GetValue<string>("POSTGRES_HOST");
        // var db = builder.Configuration.GetValue<string>("POSTGRES_DB");
        // var user = builder.Configuration.GetValue<string>("POSTGRES_USER");
        // var pass = builder.Configuration.GetValue<string>("POSTGRES_PASSWORD");
        //
        // var connectionString = $"Host={host};Database={db};Username={user};Password={pass};Port=5432; Include Error Detail=true;";

        // builder.Services.AddDbContext<HolidayDbContext>(options =>
        //     options.UseNpgsql(connectionString));

        // builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();
        // builder.Services.AddScoped<IHolidayProvider, HolidayProvider>();
        // builder.Services.AddScoped<ISubscriptionDeliver, SubscriptionDeliver>();
        //
        // builder.Services.AddSingleton<IHolidayScraper, CalendRuScraper>();

        builder.Services.AddSingleton<ITelegramBotClient>(_ =>
        {
            var token = builder.Configuration.GetValue<string>("TG_BOT_TOKEN");
            return string.IsNullOrEmpty(token) ? throw new Exception("Telegram Token is missing!") : new TelegramBotClient(token);
        });

        builder.Services.AddSingleton<IUserStateService, UserStateService>();

        builder.Services.AddTransient<IBotCommand, StartCommand>();
        builder.Services.AddTransient<IBotCommand, LeftoversCommand>();
        builder.Services.AddTransient<IBotCommand, ClaimCommand>();

        builder.Services.AddHostedService<TgBotWorker>();

        var worker = builder.Build();

        // using (var scope = worker.Services.CreateScope())
        // {
        //     var context = scope.ServiceProvider.GetRequiredService<HolidayDbContext>();
        //
        //     await context.Database.MigrateAsync();
        // }

        await worker.RunAsync();
    }
}