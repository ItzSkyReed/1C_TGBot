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
        using (_lock.EnterScope())
        {
            // Запоминаем, что юзер добавился "между" синхронизациями
            _recentlyAdded.Add(telegramId);

            var newSet = new HashSet<long>(_authorizedUsers) { telegramId };
            _authorizedUsers = newSet;
        }
    }

    public void UpdateCache(IEnumerable<long> newIds)
    {
        using (_lock.EnterScope())
        {
            var newSet = new HashSet<long>(newIds);

            foreach (var id in _recentlyAdded)
            {
                newSet.Add(id);
            }

            _authorizedUsers = newSet;

            _recentlyAdded.Clear();
        }
    }
}