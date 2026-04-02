using System.Collections.Concurrent;
using Application.Interfaces;
using Domain.Entities;

namespace Infrastructure.Services;

public class UserStateService : IUserStateService
{
    // Ключ — ChatId, Значение — текущая сессия
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    public UserSession? GetSession(long chatId) =>
        _sessions.GetValueOrDefault(chatId);

    public void SetSession(long chatId, UserSession session) =>
        _sessions[chatId] = session;

    public void ClearSession(long chatId) =>
        _sessions.TryRemove(chatId, out _);
}