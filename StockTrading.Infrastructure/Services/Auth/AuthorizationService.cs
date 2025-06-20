using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Enums;

namespace StockTrading.Infrastructure.Services.Auth;

public class AuthorizationService : IAuthorizationService
{
    public bool HasRole(UserInfo user, UserRole requiredRole)
    {
        return user.Role == requiredRole;
    }

    public bool HasRoleOrAbove(UserInfo user, UserRole minimumRole)
    {
        return user.Role >= minimumRole;
    }

    public bool IsMaster(UserInfo user)
    {
        return user.Role == UserRole.Master;
    }

    public bool IsAdminOrAbove(UserInfo user)
    {
        return user.Role >= UserRole.Admin;
    }
}