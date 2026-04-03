using Application.Interfaces;

namespace Infrastructure.Services;

public class AuthorizationCacheService : IAuthorizationCacheService
{
    private volatile HashSet<long> _authorizedUsers = [];

    // Буфер для тех, кто добавился только что
    private readonly HashSet<long> _recentlyAdded = [];

    private readonly Lock _lock = new();

    public bool IsAuthorized(long telegramId)
    {
        return _authorizedUsers.Contains(telegramId);
    }

    public void AddUser(long telegramId)
    {
        lock (_lock)
        {
            // Запоминаем, что юзер добавился "между" синхронизациями
            _recentlyAdded.Add(telegramId);

            // Добавляем в текущий кэш
            var newSet = new HashSet<long>(_authorizedUsers) { telegramId };
            _authorizedUsers = newSet;
        }
    }

    public void UpdateCache(IEnumerable<long> newIds)
    {
        lock (_lock)
        {
            var newSet = new HashSet<long>(newIds);

            // Подмешиваем тех, кто авторизовался, пока шел запрос к 1С
            foreach (var id in _recentlyAdded)
            {
                newSet.Add(id);
            }

            // Атомарно обновляем основной кэш
            _authorizedUsers = newSet;

            // Очищаем буфер. Теперь эти пользователи гарантированно
            // есть в базе 1С и придут при следующем обновлении сами.
            _recentlyAdded.Clear();
        }
    }
}