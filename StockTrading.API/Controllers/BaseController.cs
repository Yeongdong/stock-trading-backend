using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Common;

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

    private UserDto? _currentUser;

    protected async Task<UserDto> GetCurrentUserAsync()
    {
        return _currentUser ??= await UserContextService.GetCurrentUserAsync();
    }
}