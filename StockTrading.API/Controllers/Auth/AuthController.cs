using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.Features.Auth.DTOs;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.Services;

namespace StockTrading.API.Controllers.Auth;

[Route("api/[controller]")]
public class AuthController : BaseController
{
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly ICookieService _cookieService;
    private readonly IKisTokenRefreshService _kisTokenRefreshService;

    public AuthController(IConfiguration configuration, IJwtService jwtService, IUserService userService,
        IAuthService authService, IUserContextService userContextService, ICookieService cookieService,
        IKisTokenRefreshService kisTokenRefreshService) : base(userContextService)
    {
        _configuration = configuration;
        _jwtService = jwtService;
        _userService = userService;
        _authService = authService;
        _cookieService = cookieService;
        _kisTokenRefreshService = kisTokenRefreshService;
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin([FromBody] LoginRequest request)
    {
        var loginResponse = await _authService.GoogleLoginAsync(request.Credential);
        return Ok(loginResponse);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        try
        {
            var refreshResponse = await _authService.RefreshTokenAsync();
            return Ok(refreshResponse);
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var user = await GetCurrentUserAsync();
        await _authService.LogoutAsync(user.Id);

        return Ok(new { Message = "로그아웃 성공" });
    }

    [HttpGet("check")]
    public async Task<IActionResult> CheckAuth()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader == null || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { Message = "인증되지 않음" });

        var token = authHeader["Bearer ".Length..].Trim();

        var principal = _jwtService.ValidateToken(token);
        var user = await GetCurrentUserAsync();

        await _kisTokenRefreshService.EnsureValidTokenAsync(user);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = 3600,
            User = user,
            IsAuthenticated = true,
        });
    }

    [HttpPost("master-login")]
    [AllowAnonymous]
    public async Task<IActionResult> MasterLogin()
    {
        var masterUser = await _userService.GetUserByEmailAsync(_configuration["Authentication:Google:masterId"]);
        var token = _jwtService.GenerateAccessToken(masterUser);

        _cookieService.SetAuthCookie(token);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresIn = 3600,   
            User = masterUser,
            IsAuthenticated = true,
            Message = "마스터 로그인 성공"
        });
    }
}