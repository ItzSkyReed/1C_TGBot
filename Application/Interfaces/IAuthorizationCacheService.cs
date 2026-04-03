namespace Application.Interfaces;

public interface IAuthorizationCacheService
{
    public bool IsAuthorized(long telegramId);
    public void UpdateCache(IEnumerable<long> newIds);
    public void AddUser(long telegramId);
}