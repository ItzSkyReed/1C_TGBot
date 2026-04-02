using Application.Interfaces;
using TelegramBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Commands;
// TODO: Здесь сейчас вообще не то)))
public class StartCommand(IOneCService oneCService) : IBotCommand
{
    public string CommandName => "/start";


    public async Task ExecuteAsync(Message message, ITelegramBotClient bot, CancellationToken cancellationToken)
    {
        var leftovers = await oneCService.GetComponentCategoriesAsync(cancellationToken);

        string responseText;

        if (leftovers.Count != 0)
        {
            var items = leftovers.Select(x => $"*{Markdown.Escape(x.Name)}*\n");
            responseText = string.Join("\n\n", items);
        }
        else
            responseText = "В 1С пока нет доступных остатков или список пуст.";

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: responseText,
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: cancellationToken
        );
    }
}