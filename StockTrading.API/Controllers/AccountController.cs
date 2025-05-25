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
    private readonly ILogger<AccountController> _logger;

    public AccountController(IKisService kisService, IUserService userService, IGoogleAuthProvider googleAuthProvider, ILogger<AccountController> logger)
    {
        _kisService = kisService;
        _userService = userService;
        _googleAuthProvider = googleAuthProvider;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("userInfo")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateUserInfo([FromBody] UserInfoRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userInfo = await _googleAuthProvider.GetUserInfoAsync(User);
            var user = await _userService.GetUserByEmailAsync(userInfo.Email);

            var result = await _kisService.UpdateUserKisInfoAndTokensAsync(user.Id, request.AppKey, request.AppSecret,
                request.AccountNumber);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "ArgumentException 발생");
            return BadRequest(ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HttpRequestException 발생");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception 발생");
            return StatusCode(500, ex.Message);
        }
    }
}