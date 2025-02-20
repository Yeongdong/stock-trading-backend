using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IKisService _kisService;
    private readonly IUserService _userService;
    private readonly IGoogleAuthProvider _googleAuthProvider;

    public AccountController(IKisService kisService, IUserService userService, IGoogleAuthProvider googleAuthProvider)
    {
        _kisService = kisService;
        _userService = userService;
        _googleAuthProvider = googleAuthProvider;
    }

    [Authorize]
    [HttpPost("userInfo")]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UserInfoRequest request)
    {
        try
        {
            var userInfo = await _googleAuthProvider.GetUserInfoAsync(User);
            var user = await _userService.GetUserByEmail(userInfo.Email);

            var result = await _kisService.UpdateUserKisInfoAndTokensAsync(user.Id, request.AppKey, request.AppSecret,
                request.AccountNumber);

            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}