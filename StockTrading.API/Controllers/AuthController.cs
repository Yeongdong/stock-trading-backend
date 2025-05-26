using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StockTrading.API.DTOs.Requests;
using StockTrading.API.Services;
using StockTrading.API.Validator.Interfaces;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;

namespace StockTrading.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IGoogleAuthValidator _googleAuthValidator;
    private readonly IUserContextService _userContextService;
    private readonly JwtSettings _jwtSettings;

    public AuthController(IConfiguration configuration, IJwtService jwtService, IUserService userService,
        IGoogleAuthValidator googleAuthValidator, IUserContextService userContextService,
        IOptions<JwtSettings> jwtSettings)
    {
        _configuration = configuration;
        _jwtService = jwtService;
        _userService = userService;
        _googleAuthValidator = googleAuthValidator;
        _userContextService = userContextService;
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var payload = await _googleAuthValidator.ValidateAsync(
            request.Credential,
            _configuration["Authentication:Google:ClientId"]);

        var user = await _userService.GetOrCreateGoogleUserAsync(payload);
        var token = _jwtService.GenerateToken(user);

        SetAuthCookie(token);
        return Ok(new { User = user });
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

    [HttpGet("check")]
    public async Task<IActionResult> CheckAuth()
    {
        if (!Request.Cookies.TryGetValue("auth_token", out var token))
        {
            return Unauthorized(new { Message = "인증되지 않음" });
        }

        var principal = _jwtService.ValidateToken(token);
        var user = await _userContextService.GetCurrentUserAsync();

        return Ok(new
        {
            IsAuthenticated = true,
            User = user
        });
    }

    private void SetAuthCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Path = "/"
        };

        Response.Cookies.Append("auth_token", token, cookieOptions);
    }
}