using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using stock_trading_backend.DTOs;
using stock_trading_backend.Interfaces;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Settings;

namespace stock_trading_backend.controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IGoogleAuthValidator _googleAuthValidator;
    private readonly JwtSettings _jwtSettings;


    public AuthController(IConfiguration configuration, IJwtService jwtService, IUserService userService,
        IGoogleAuthValidator googleAuthValidator, IOptions<JwtSettings> jwtSettings)
    {
        _configuration = configuration;
        _jwtService = jwtService;
        _userService = userService;
        _googleAuthValidator = googleAuthValidator;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var payload = await _googleAuthValidator.ValidateAsync(request.Credential,
                _configuration["Authentication:Google:ClientId"]);
            var user = await _userService.GetOrCreateGoogleUserAsync(payload);
            var token = _jwtService.GenerateToken(user);

            SetAuthCookie(token);

            return Ok(new { User = user });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("auth_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        });

        return Ok(new { Message = "로그아웃 성공" });
    }

    private void SetAuthCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
        };

        Response.Cookies.Append("auth_token", token, cookieOptions);
    }
}