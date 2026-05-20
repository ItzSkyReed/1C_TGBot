using Telegram.Bot.Extensions;
using TelegramBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Application.Interfaces;

namespace TelegramBot.Commands;

public class LeftoversCommand(IOneCService oneCService) : IBotCommand
{
    public string CommandName => "/leftovers";

    public async Task ExecuteAsync(Message message, ITelegramBotClient bot, CancellationToken ct)
    {
        var categories = await oneCService.GetComponentCategoriesAsync(ct);

        if (categories.Count == 0)
        {
            await bot.SendMessage(message.Chat.Id, Markdown.Escape("📭 В 1С пока не создано ни одной категории товаров."), cancellationToken: ct);
            return;
        }

        var text = $"*{Markdown.Escape("📂 Выберите категорию из 1С:")}*";

        // Формируем кнопки категорий (по 2 в ряд)
        var buttons = categories.Select(cat =>
            InlineKeyboardButton.WithCallbackData(cat.Name, $"/leftovers:{cat.Id}")
        ).Chunk(2);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: ct);
    }

    public async Task ExecuteAsync(CallbackQuery query, ITelegramBotClient bot, CancellationToken ct)
    {
        var parts = query.Data!.Split(':');
        // parts[0] = "/leftovers"
        // parts[1] = id_категории

        if (parts.Length < 2) return;

        var categoryId = parts[1];

        var categoryName = query.Message?.ReplyMarkup?.InlineKeyboard
            .SelectMany(row => row)
            .FirstOrDefault(button => button.CallbackData == query.Data)?
            .Text ?? "Выбранная категория";

        var leftovers = await oneCService.GetLeftoversByCategoryIdAsync(categoryId, ct);

        string responseText;
        if (leftovers.Count != 0)
        {
            var items = leftovers.Select(x =>
                $"📦 *{Markdown.Escape(x.Name)}*\n" +
                $" Остаток: `{x.Balance}` шт\\.");

            responseText = $"✅ *Остатки в категории \"{Markdown.Escape(categoryName)}\":*\n\n" +
                           string.Join("\n\n", items);
        }
        else
        {
            responseText = Markdown.Escape("ℹ️ В этой категории сейчас нет товаров с остатками.");
        }

        await bot.EditMessageText(
            chatId: query.Message!.Chat.Id,
            messageId: query.Message.MessageId,
            text: responseText,
            parseMode: ParseMode.MarkdownV2,
            cancellationToken: ct);

        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
    }
}