using Domain.Entities;

namespace Application.Interfaces;

public interface IUserStateService
{
    UserSession? GetSession(long chatId);
    void SetSession(long chatId, UserSession session);
    void ClearSession(long chatId);
}