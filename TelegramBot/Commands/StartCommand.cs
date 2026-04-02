using Application.Interfaces;
using Domain.Entities;
using TelegramBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBot.Commands;

public class StartCommand(IOneCService oneCService, IUserStateService stateService) : IBotCommand
{
    public string CommandName => "/start";

    public async Task ExecuteAsync(Message message, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        if (message.Text != null && message.Text.StartsWith(CommandName))
        {
            await HandleStartAsync(message, bot, ct);
            return;
        }

        var session = stateService.GetSession(chatId);
        if (session != null && session.ActiveCommand == CommandName && session.CurrentStep == "WaitingForAuthCode")
        {
            await HandleAuthCodeStepAsync(message, bot, ct);
        }
    }

    private async Task HandleStartAsync(Message message, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = message.Chat.Id;

        var session = new UserSession
        {
            ActiveCommand = CommandName,
            CurrentStep = "WaitingForAuthCode"
        };
        stateService.SetSession(chatId, session);

        await bot.SendMessage(
            chatId: chatId,
            text: "👋 *Добро пожаловать!*\n\nДля доступа к боту, введите ваш *код авторизации* сгенерированный в 1С:",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct
        );
    }

    private async Task HandleAuthCodeStepAsync(Message message, ITelegramBotClient bot, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.Text)) return;

        var chatId = message.Chat.Id;
        var rawText = message.Text.Trim();

        if (!uint.TryParse(rawText, out var parsedIdentifier))
        {
            await bot.SendMessage(
                chatId: chatId,
                text: "⚠️ Код авторизации должен состоять только из цифр.\nПожалуйста, введите корректный код:",
                cancellationToken: ct);
            return;
        }

        var name = $"{message.From!.FirstName} {message.From.LastName}".Trim();

        var user = new UserAuthDto
        {
            TelegramId = message.From.Id,
            Identifier = parsedIdentifier,
            Name = name
        };
        try
        {
            var isSuccess = await oneCService.AuthorizeUserAsync(user, ct);

            if (isSuccess)
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "✅ *Авторизация прошла успешно!*\n\nТеперь вам доступны функции бота",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);

                stateService.ClearSession(chatId);
            }
            else
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "❌ *Неверный или просроченный код.*\n\nПопробуйте ввести его еще раз:",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct);
            }
        }
        catch (Exception)
        {
            await bot.SendMessage(
                chatId: chatId,
                text: "⚠️ Произошла ошибка при соединении с сервером 1С",
                cancellationToken: ct);
        }
    }
}