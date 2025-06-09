using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Services;

namespace StockTrading.API.Controllers.User;

[ApiController]
[Route("api/[controller]")]
public class AccountController : BaseController
{
    private readonly IKisTokenService _kisTokenService;

    public AccountController(IKisTokenService kisTokenService, IUserContextService userContextService) : base(userContextService)
    {
        _kisTokenService = kisTokenService;
    }

    [HttpPost("userInfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UserSettingsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await GetCurrentUserAsync();

        var result = await _kisTokenService.UpdateKisCredentialsAndTokensAsync(
            user.Id,
            request.AppKey,
            request.AppSecret,
            request.AccountNumber);

        return Ok(result);
    }
}