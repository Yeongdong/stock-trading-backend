using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Interfaces;

namespace stock_trading_backend.controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;

    public AuthController(IConfiguration configuration, IJwtService jwtService, IUserService userService)
    {
        _configuration = configuration;
        _jwtService = jwtService;
        _userService = userService;
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            var payload = await ValidateGoogleToken(request.Credential);
            var user = await _userService.GetOrCreateGoogleUser(payload);
            var token = _jwtService.GenerateToken(user);
    
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // [HttpGet("oauth2/callback/google")]
    // public async Task<IActionResult> GoogleCallback()
    // {
    //     try
    //     {
    //         var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
    //
    //         Console.WriteLine($"Authentication succeeded: {authenticateResult.Succeeded}");
    //
    //         if (!authenticateResult.Succeeded)
    //         {
    //             Console.WriteLine($"Authentication failed: {authenticateResult.Failure?.Message}");
    //             return BadRequest("Google authentication failed");
    //         }
    //
    //         var email = authenticateResult.Principal.FindFirst(ClaimTypes.Email)?.Value;
    //         var name = authenticateResult.Principal.FindFirst(ClaimTypes.Name)?.Value;
    //
    //         Console.WriteLine($"Retrieved email: {email}");
    //         Console.WriteLine($"Retrieved name: {name}");
    //
    //         if (string.IsNullOrEmpty(email))
    //         {
    //             return BadRequest("Email not provided by Google");
    //         }
    //
    //         var jwtToken = _jwtService.GenerateToken(email, name);
    //         Console.WriteLine($"Generated JWT token: {jwtToken?.Substring(0, 20)}..."); // 토큰의 일부만 출력
    //         Console.WriteLine(
    //             $"Redirect URL: {_configuration["Frontend:Url"]}/oauth/callback?token={jwtToken?.Substring(0, 20)}...");
    //
    //         var redirectUrl = $"{_configuration["Frontend:Url"]}/oauth/callback?token={jwtToken}";
    //         Console.WriteLine($"Redirecting to: {redirectUrl}");
    //
    //         return Redirect(redirectUrl);
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Exception in GoogleCallback: {ex}");
    //         return StatusCode(500, "Internal server error during authentication");
    //     }
    // }

    private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleToken(string token)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] }
            };
        
            return await GoogleJsonWebSignature.ValidateAsync(token, settings);
        }
        catch(Exception ex)
        {
            throw new Exception("Failed to validate Google token", ex);
        }
    }


    // [HttpPost("refresh")]
    // public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] string refreshToken)
    // {
    //     var response = await _authService.RefreshTokenAsync(refreshToken);
    //     return Ok(response);
    // }
}