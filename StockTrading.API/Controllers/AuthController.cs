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
            var user = await _userService.GetOrCreateGoogleUserAsync(payload);
            var token = _jwtService.GenerateToken(user);
    
            return Ok(new GoogleLoginResponse(user, token));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
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
}