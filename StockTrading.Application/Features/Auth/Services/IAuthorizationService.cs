using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Enums;

namespace StockTrading.Application.Features.Auth.Services;

public interface IAuthorizationService
{
    bool HasRole(UserInfo user, UserRole requiredRole);
    bool HasRoleOrAbove(UserInfo user, UserRole minimumRole);
    bool IsMaster(UserInfo user);
    bool IsAdminOrAbove(UserInfo user);
}