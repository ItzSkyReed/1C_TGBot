using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Interfaces;

public interface IBotCommand
{
    string CommandName { get; }

    Task ExecuteAsync(Message message, ITelegramBotClient bot, CancellationToken cancellationToken)
        => Task.CompletedTask;

    Task ExecuteAsync(CallbackQuery query, ITelegramBotClient bot, CancellationToken cancellationToken)
        => Task.CompletedTask;
}