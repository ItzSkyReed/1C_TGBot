using Application.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Interfaces;

namespace TelegramBot;

public class TgBotWorker(ITelegramBotClient bot, IServiceScopeFactory scopeFactory, ILogger<TgBotWorker> logger) : BackgroundService
{
    private string? _botUsername;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RegisterCommands(bot);
        logger.LogInformation("Commands added");

        var me = await bot.GetMe(cancellationToken);
        _botUsername = me.Username;

        await base.StartAsync(cancellationToken);
    }

    private static async Task RegisterCommands(ITelegramBotClient botClient)
    {
        var commands = new[]
        {
            new BotCommand { Command = "start", Description = "Запустить бота" },
            new BotCommand { Command = "leftovers", Description = "Просмотреть остатки" },
            new BotCommand { Command = "claim", Description = "Добавить претензию" }
        };

        await botClient.SetMyCommands(commands);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
            DropPendingUpdates = true
        };

        bot.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, receiverOptions, stoppingToken);
        logger.LogInformation("TG bot started");
        await Task.Delay(-1, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var commands = scope.ServiceProvider.GetServices<IBotCommand>();

            var stateService = scope.ServiceProvider.GetRequiredService<IUserStateService>();

            var task = update switch
            {
                { Message: { } msg } => ProcessMessage(msg, commands, stateService, ct),
                { CallbackQuery: { Data: { } data } query } => ProcessCallback(query, data, commands, ct),
                _ => Task.CompletedTask
            };

            await task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке обновления {UpdateId}", update.Id);
        }
    }

    private async Task ProcessMessage(Message msg, IEnumerable<IBotCommand> commands, IUserStateService stateService, CancellationToken ct)
    {
        var chatId = msg.Chat.Id;
        var session = stateService.GetSession(chatId);

        var botCommands = commands.ToList();
        if (session != null)
        {
            var activeCommand = botCommands.FirstOrDefault(x =>
                x.CommandName.Equals(session.ActiveCommand, StringComparison.OrdinalIgnoreCase));

            if (activeCommand != null)
            {
                logger.LogInformation("Перехват сообщения для активной сессии {Command}", session.ActiveCommand);
                await activeCommand.ExecuteAsync(msg, bot, ct);
                return;
            }
        }

        var text = msg.Text ?? msg.Caption;

        // Если это не текст или он не начинается со слэша — игнорируем
        if (string.IsNullOrWhiteSpace(text) || !text.StartsWith('/'))
        {
            return;
        }

        var atIndex = text.IndexOf('@');
        var baseCommand = text;
        if (atIndex != -1)
        {
            baseCommand = text[..atIndex].Trim();
            var targetBotName = text[(atIndex + 1)..].Trim();

            if (!targetBotName.Equals(_botUsername, StringComparison.OrdinalIgnoreCase)) return;
        }

        var command = botCommands.FirstOrDefault(x =>
            baseCommand.Equals(x.CommandName, StringComparison.OrdinalIgnoreCase));

        logger.LogWarning("{FromUsername} wrote command {UserCommand}, {BotCommand} was found", msg.From?.Username, text, command?.CommandName);

        if (command != null)
        {
            await command.ExecuteAsync(msg, bot, ct);
        }
    }

    private async Task ProcessCallback(CallbackQuery query, string buttonCommand, IEnumerable<IBotCommand> commands, CancellationToken ct)
    {
        var colonIndex = buttonCommand.IndexOf(':');
        var baseCommand = colonIndex != -1
            ? buttonCommand[..colonIndex].Trim()
            : buttonCommand.Trim();

        var command = commands.FirstOrDefault(x =>
            baseCommand.Equals(x.CommandName, StringComparison.OrdinalIgnoreCase));

        logger.LogWarning("{FromUsername} used inline command {UserCommand}, {BotCommand} was found", query.From.Username, buttonCommand, command?.CommandName);

        if (command != null)
        {
            await command.ExecuteAsync(query, bot, ct);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine("API ERROR: " + exception.Message);
        return Task.CompletedTask;
    }
}