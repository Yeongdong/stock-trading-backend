using Microsoft.AspNetCore.Mvc;
using StockTrading.API.DTOs.Requests;
using StockTrading.API.Services;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : BaseController
{
    private readonly IKisService _kisService;

    public AccountController(IKisService kisService, IUserContextService userContextService) : base(userContextService)
    {
        _kisService = kisService;
    }

    [HttpPost("userInfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UserInfoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await GetCurrentUserAsync();

        var result = await _kisService.UpdateUserKisInfoAndTokensAsync(
            user.Id,
            request.AppKey,
            request.AppSecret,
            request.AccountNumber);

        return Ok(result);
    }
}