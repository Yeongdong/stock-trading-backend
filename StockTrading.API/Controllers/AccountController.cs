using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.Repositories;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IKoreaInvestmentService _koreaInvestmentService;
    private readonly IKisService _kisService;
    private readonly IGoogleAuthProvider _googleAuthProvider;
    private readonly ApplicationDbContext _context;

    public AccountController(IKoreaInvestmentService koreaInvestmentService, IKisService kisService, IGoogleAuthProvider googleAuthProvider, ApplicationDbContext context)
    {
        _koreaInvestmentService = koreaInvestmentService;
        _kisService = kisService;
        _googleAuthProvider = googleAuthProvider;
        _context = context;
    }

    [Authorize]
    [HttpPost("token")]
    public async Task<IActionResult> GetToken([FromBody] KisTokenRequest request)
    {
        try
        {
            var userInfo = await _googleAuthProvider.GetUserInfoAsync(User);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);
            var result = await _koreaInvestmentService.GetTokenAsync(request.AppKey, request.AppSecret);
            var expiresIn = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
            
            await _kisService.SaveTokenAsync(
                user.Id,
                result.AccessToken,
                expiresIn,
                result.TokenType);
            return Ok(new
            {
                AccessToken = result.AccessToken,
                ExpiresIn = result.ExpiresIn,
                TokenType = result.TokenType
            });
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