using Application;
using Application.Interfaces;
using Domain.Entities;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Interfaces;

namespace TelegramBot.Commands;

public class ClaimCommand(IUserStateService stateService, IOneCService oneCService) : IBotCommand
{
    public string CommandName => "/claim";

    public async Task ExecuteAsync(Message message, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var session = stateService.GetSession(chatId);

        if (session == null || session.ActiveCommand != CommandName)
        {
            await HandleStartAsync(chatId, bot, ct);
            return;
        }

        switch (session.CurrentStep)
        {
            case ClaimSteps.WaitingForComponent:
                await HandleComponentStepAsync(message, session, bot, ct);
                break;
            case ClaimSteps.WaitingForSupplier:
                await HandleSupplierStepAsync(message, session, bot, ct);
                break;
            case ClaimSteps.WaitingForDescription:
                await HandleDescriptionStepAsync(message, session, bot, ct);
                break;
            case ClaimSteps.WaitingForPhoto:
                await HandlePhotoStepAsync(message, session, bot, ct);
                break;
        }
    }

    public async Task ExecuteAsync(CallbackQuery query, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = query.Message!.Chat.Id;
        var session = stateService.GetSession(chatId);

        if (session == null || session.ActiveCommand != CommandName)
        {
            await bot.AnswerCallbackQuery(query.Id, "Сессия устарела. Начните заново через /claim", cancellationToken: ct);
            return;
        }

        var parts = query.Data!.Split(':');
        var action = parts.Length > 1 ? parts[1] : string.Empty;

        switch (action)
        {
            case "skip_photo":
                await HandleSkipPhotoCallbackAsync(query, session, bot, ct);
                break;
            case "send":
                await HandleSendCallbackAsync(query, bot, ct);
                break;
            case "cancel":
                await HandleCancelCallbackAsync(query, bot, ct);
                break;
        }

        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);
    }


    private async Task HandleStartAsync(long chatId, ITelegramBotClient bot, CancellationToken ct)
    {
        var session = new UserSession
        {
            ActiveCommand = CommandName,
            CurrentStep = new ClaimSteps.WaitingForComponent()
        };
        stateService.SetSession(chatId, session);

        await bot.SendMessage(chatId, $"🛠 *Создание претензии*\n{Markdown.Escape("Напишите название комплектующей (например, RTX 3050):")}", parseMode: ParseMode.MarkdownV2,
            cancellationToken: ct);
    }

    private async Task HandleComponentStepAsync(Message message, UserSession session, ITelegramBotClient bot, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.Text)) return;

        var userInput = message.Text;
        List<Component> similarComponents;

        try
        {
            similarComponents = await oneCService.GetSimilarComponentName(userInput, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await bot.SendMessage(message.Chat.Id,
                "❌ В базе ничего не найдено по этому запросу.\nПопробуйте написать название иначе:",
                cancellationToken: ct);
            return;
        }
        catch (Exception)
        {
            await bot.SendMessage(message.Chat.Id,
                "⚠️ Произошла ошибка при подключение с 1С.",
                cancellationToken: ct);
            return;
        }

        if (similarComponents.Count == 0)
        {
            await bot.SendMessage(message.Chat.Id,
                "❌ Комплектующие не найдены. Попробуйте уточнить запрос:",
                cancellationToken: ct);
            return;
        }

        var candidates = similarComponents.Select(c => c.Name).ToList();

        var bestMatchIndex = Utils.GetBestMatchIndex(userInput, candidates);

        if (bestMatchIndex == -1)
        {
            await bot.SendMessage(message.Chat.Id,
                "Некоторые позиции были найдены, но они слишком слабо похожи на введенное вами. Введите название точнее:",
                cancellationToken: ct);
            return;
        }

        var bestComponent = similarComponents[bestMatchIndex];

        var data = session.GetData<ClaimData>();

        data.ComponentName = bestComponent.Name;
        data.ComponentId = bestComponent.Id;

        session.SetData(data);
        session.CurrentStep = new ClaimSteps.WaitingForSupplier();
        stateService.SetSession(message.Chat.Id, session);

        await bot.SendMessage(message.Chat.Id,
            $"✅ Найдена позиция: *{bestComponent.Name}*\n\nТеперь напишите *поставщика*:",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandleSupplierStepAsync(Message message, UserSession session, ITelegramBotClient bot, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.Text)) return;

        var userInput = message.Text;
        List<Supplier> similarSuppliers;

        try
        {
            similarSuppliers = await oneCService.GetSimilarSupplierName(userInput, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await bot.SendMessage(message.Chat.Id,
                "❌Не найдено поставщиков по этому запросу.\nПопробуйте написать название иначе:",
                cancellationToken: ct);
            return;
        }
        catch (Exception)
        {
            await bot.SendMessage(message.Chat.Id,
                "⚠️ Произошла ошибка при подключении к 1С.",
                cancellationToken: ct);
            return;
        }

        if (similarSuppliers.Count == 0)
        {
            await bot.SendMessage(message.Chat.Id,
                "❌ Поставщики не найдены. Попробуйте уточнить запрос:",
                cancellationToken: ct);
            return;
        }

        var candidates = similarSuppliers.Select(s => s.Name).ToList();
        var bestMatchIndex = Utils.GetBestMatchIndex(userInput, candidates);

        if (bestMatchIndex == -1)
        {
            await bot.SendMessage(message.Chat.Id,
                "Некоторые поставщики были найдены, но они слишком слабо похожи на введенное вами. Введите название точнее:",
                cancellationToken: ct);
            return;
        }

        var bestSupplier = similarSuppliers[bestMatchIndex];

        var data = session.GetData<ClaimData>();

        data.SupplierName = bestSupplier.Name;
        data.SupplierId = bestSupplier.Id;

        session.SetData(data);
        session.CurrentStep = new ClaimSteps.WaitingForDescription();
        stateService.SetSession(message.Chat.Id, session);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: $"✅ Поставщик: *{data.SupplierName}*\n\nОпишите претензию:",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandleDescriptionStepAsync(Message message, UserSession session, ITelegramBotClient bot, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.Text)) return;

        var data = session.GetData<ClaimData>();

        data.Description = message.Text;

        session.SetData(data);

        session.CurrentStep = new ClaimSteps.WaitingForPhoto();
        stateService.SetSession(message.Chat.Id, session);

        var skipMarkup = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithCallbackData("⏭ Без фото", $"{CommandName}:skip_photo"));

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "✅ Описание принято.\n\nТеперь отправьте *фото дефекта* или нажмите кнопку ниже, чтобы пропустить этот шаг:",
            replyMarkup: skipMarkup,
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task HandlePhotoStepAsync(Message message, UserSession session, ITelegramBotClient bot, CancellationToken ct)
    {
        if (message.Photo is { Length: > 0 })
        {
            var data = session.GetData<ClaimData>();
            data.PhotoFileId = message.Photo.Last().FileId;

            session.SetData(data);
            session.CurrentStep = new ClaimSteps.WaitingForConfirmation();
            stateService.SetSession(message.Chat.Id, session);

            await SendConfirmationAsync(message.Chat.Id, data, bot, ct);
        }
        else
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Пожалуйста, отправьте именно картинку (фото) или нажмите 'Без фото'.", cancellationToken: ct);
        }
    }

    private async Task HandleSkipPhotoCallbackAsync(CallbackQuery query, UserSession session, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = query.Message!.Chat.Id;

        session.CurrentStep = new ClaimSteps.WaitingForConfirmation();
        stateService.SetSession(chatId, session);

        var data = session.GetData<ClaimData>();

        await bot.EditMessageReplyMarkup(chatId, query.Message.MessageId, replyMarkup: null, cancellationToken: ct);
        await SendConfirmationAsync(chatId, data, bot, ct);
    }

    private async Task HandleSendCallbackAsync(CallbackQuery query, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = query.Message!.Chat.Id;
        var session = stateService.GetSession(chatId);

        if (session == null)
        {
            await bot.EditMessageText(chatId, query.Message.MessageId, "⏳ Сессия устарела. Начните заново.", cancellationToken: ct);
            return;
        }

        var data = session.GetData<ClaimData>();

        try
        {
            await bot.EditMessageText(chatId, query.Message.MessageId, "⏳ Обработка данных и отправка в 1С...", cancellationToken: ct);

            if (!string.IsNullOrEmpty(data.PhotoFileId))
            {
                var file = await bot.GetFile(data.PhotoFileId, ct);

                using var ms = new MemoryStream();
                if (file.FilePath != null)
                    await bot.DownloadFile(file.FilePath, ms, ct);

                data.PhotoBase64 = Convert.ToBase64String(ms.ToArray());
            }

            await oneCService.SendClaimAsync(data, ct);

            await bot.EditMessageText(chatId, query.Message.MessageId, "✅ Претензия успешно создана", cancellationToken: ct);

            stateService.ClearSession(chatId);
        }
        catch (Exception)
        {
            await bot.EditMessageText(chatId, query.Message.MessageId, "⚠️ Произошла ошибка при отправке претензии в 1С. ", cancellationToken: ct);
        }
    }

    private async Task HandleCancelCallbackAsync(CallbackQuery query, ITelegramBotClient bot, CancellationToken ct)
    {
        var chatId = query.Message!.Chat.Id;
        await bot.EditMessageText(chatId, query.Message.MessageId, "❌ Создание претензии отменено.", cancellationToken: ct);
        stateService.ClearSession(chatId);
    }

    private async Task SendConfirmationAsync(long chatId, ClaimData data, ITelegramBotClient bot, CancellationToken ct)
    {
        var photoText = data.PhotoFileId != null ? "Прикреплено 🖼" : "Нет ❌";
        var text = $"*Подтвердите данные претензии:*\n\n" +
                   $"📦 *Деталь:* {data.ComponentName}\n" +
                   $"🏢 *Поставщик:* {data.SupplierName}\n" +
                   $"📸 *Фото:* {photoText}";

        var buttons = new InlineKeyboardMarkup([
            [InlineKeyboardButton.WithCallbackData("✅ Создать претензию", $"{CommandName}:send")],
            [InlineKeyboardButton.WithCallbackData("❌ Отмена", $"{CommandName}:cancel")]
        ]);

        await bot.SendMessage(chatId, text, replyMarkup: buttons, parseMode: ParseMode.Markdown, cancellationToken: ct);
    }
}