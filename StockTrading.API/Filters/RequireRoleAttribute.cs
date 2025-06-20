using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StockTrading.API.Services;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Domain.Enums;

namespace StockTrading.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAsyncActionFilter
{
    private readonly UserRole _minimumRole;
    private readonly bool _exactMatch;

    public RequireRoleAttribute(UserRole minimumRole, bool exactMatch = false)
    {
        _minimumRole = minimumRole;
        _exactMatch = exactMatch;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userContextService = context.HttpContext.RequestServices.GetService<IUserContextService>();
        var authorizationService = context.HttpContext.RequestServices.GetService<IAuthorizationService>();

        if (userContextService == null || authorizationService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        try
        {
            var user = await userContextService.GetCurrentUserAsync();

            bool hasPermission = _exactMatch
                ? authorizationService.HasRole(user, _minimumRole)
                : authorizationService.HasRoleOrAbove(user, _minimumRole);

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new { message = "접근 권한이 없습니다." })
                {
                    StatusCode = 403
                };
                return;
            }

            await next();
        }
        catch (UnauthorizedAccessException)
        {
            context.Result = new ObjectResult(new { message = "인증이 필요합니다." })
            {
                StatusCode = 401
            };
        }
        catch (Exception)
        {
            context.Result = new StatusCodeResult(500);
        }
    }
}