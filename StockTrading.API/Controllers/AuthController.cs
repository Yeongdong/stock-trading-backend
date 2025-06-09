using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.API.Validator.Interfaces;
using StockTrading.Application.DTOs.Auth;
using StockTrading.Application.Services;

namespace StockTrading.API.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IGoogleAuthValidator _googleAuthValidator;
    private readonly ICookieService _cookieService;
    private readonly IKisTokenRefreshService _kisTokenRefreshService;

    public AuthController(IConfiguration configuration, IJwtService jwtService, IUserService userService,
        IGoogleAuthValidator googleAuthValidator, IUserContextService userContextService,
        ICookieService cookieService, IKisTokenRefreshService kisTokenRefreshService) : base(userContextService)
    {
        _configuration = configuration;
        _jwtService = jwtService;
        _userService = userService;
        _googleAuthValidator = googleAuthValidator;
        _cookieService = cookieService;
        _kisTokenRefreshService = kisTokenRefreshService;
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] LoginRequest request)
    {
        var payload = await _googleAuthValidator.ValidateAsync(
            request.Credential,
            _configuration["Authentication:Google:ClientId"]);

        var user = await _userService.CreateOrGetGoogleUserAsync(payload);
        var token = _jwtService.GenerateToken(user);

        _cookieService.SetAuthCookie(token);

        return Ok(new LoginResponse
        {
            User = user,
            Message = "로그인 성공"
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _cookieService.DeleteAuthCookie();

        return Ok(new { Message = "로그아웃 성공" });
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckAuth()
    {
        var token = _cookieService.GetAuthToken();
        if (token == null)
            return Unauthorized(new { Message = "인증되지 않음" });

        var principal = _jwtService.ValidateToken(token);
        var user = await GetCurrentUserAsync();

        await _kisTokenRefreshService.EnsureValidTokenAsync(user);

        return Ok(new LoginResponse
        {
            User = user,
            IsAuthenticated = true,
        });
    }
}