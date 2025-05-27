using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.API.Controllers;

[ApiController]
[Authorize]
public class BaseController : ControllerBase
{
    protected readonly IUserContextService UserContextService;

    public BaseController(IUserContextService userContextService)
    {
        UserContextService = userContextService;
    }

    private UserInfo? _currentUser;

    protected async Task<UserInfo> GetCurrentUserAsync()
    {
        return _currentUser ??= await UserContextService.GetCurrentUserAsync();
    }
}