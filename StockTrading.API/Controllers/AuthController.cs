using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using stock_trading_backend.DTOs;
using stock_trading_backend.Interfaces;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Exceptions.Authentication;
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
    
    [HttpGet("check")]
    public async Task<IActionResult> CheckAuth()
    {
        try
        {
            if (!Request.Cookies.TryGetValue("auth_token", out var token))
            {
                return Unauthorized(new { Message = "인증되지 않음" });
            }

            var principal = _jwtService.ValidateToken(token);
            if (principal == null)
            {
                return Unauthorized(new { Message = "유효하지 않은 토큰" });
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { Message = "이메일 정보 없음" });
            }

            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                return Unauthorized(new { Message = "사용자 정보 없음" });
            }

            return Ok(new { 
                IsAuthenticated = true,
                User = user
            });
        }
        catch (TokenValidationException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "인증 확인 중 오류 발생", Error = ex.Message });
        }
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