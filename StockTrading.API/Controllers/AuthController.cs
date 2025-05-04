using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using stock_trading_backend.DTOs;
using stock_trading_backend.Interfaces;
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
    private readonly IGoogleAuthValidator _googleAuthValidator;

    public AuthController(IConfiguration configuration, IJwtService jwtService, IUserService userService,
        IGoogleAuthValidator googleAuthValidator)
    {
        _configuration = configuration;
        _jwtService = jwtService;
        _userService = userService;
        _googleAuthValidator = googleAuthValidator;
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

            return Ok(new GoogleLoginResponse(user, token));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}