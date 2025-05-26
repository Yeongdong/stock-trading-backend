using Microsoft.AspNetCore.Mvc;
using StockTrading.API.DTOs.Requests;
using StockTrading.API.Services;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IKisService _kisService;
    private readonly IUserContextService _userContextService;

    public AccountController(IKisService kisService, IUserContextService userContextService)
    {
        _kisService = kisService;
        _userContextService = userContextService;
    }

      [HttpPost("userInfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UserInfoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userContextService.GetCurrentUserAsync();
        
        var result = await _kisService.UpdateUserKisInfoAndTokensAsync(
            user.Id, 
            request.AppKey, 
            request.AppSecret,
            request.AccountNumber);

        return Ok(result);
    }
}