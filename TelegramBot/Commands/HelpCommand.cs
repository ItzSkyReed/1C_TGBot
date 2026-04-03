using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Interfaces;

namespace TelegramBot.Commands;

public class HelpCommand : IBotCommand
{
    public string CommandName => "/help";

    public async Task ExecuteAsync(Message message, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = message.Chat.Id;

        const string helpText = "🤖 *Доступные команды:*\n\n" +
                                "📦 /leftovers — просмотр остатков товаров на складе по категориям.\n\n" +
                                "📝 /claim — создание претензии о факте некачественной комплектующей.";

        await bot.SendMessage(
            chatId: chatId,
            text: helpText,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }
}