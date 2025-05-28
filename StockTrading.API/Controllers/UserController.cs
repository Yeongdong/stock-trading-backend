using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class UserController : BaseController
{
    public UserController(IUserContextService userContextService)
        : base(userContextService)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await GetCurrentUserAsync();
        return Ok(user);
    }
}