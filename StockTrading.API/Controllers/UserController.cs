using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;

namespace StockTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class UserController : ControllerBase
{
    private readonly IUserContextService _userContextService;

    public UserController(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await _userContextService.GetCurrentUserAsync();
        return Ok(user);
    }
}