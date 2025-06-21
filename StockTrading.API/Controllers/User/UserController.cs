using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;

namespace StockTrading.API.Controllers.User;

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
        Response.Headers.Add("Access-Control-Allow-Origin", "https://happy-glacier-0243a741e.6.azurestaticapps.net");
        Response.Headers.Add("Access-Control-Allow-Credentials", "true");
        
        var user = await GetCurrentUserAsync();
        return Ok(user);
    }
}