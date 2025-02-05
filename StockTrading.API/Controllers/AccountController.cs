using Microsoft.AspNetCore.Mvc;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IKoreaInvestmentService _koreaInvestmentService;

    public AccountController(IKoreaInvestmentService koreaInvestmentService)
    {
        _koreaInvestmentService = koreaInvestmentService; 
    }

    [HttpPost("token")]
    public async Task<IActionResult> GetToken([FromBody]string appKey, string appSecret)
    {
        try
        {
            var result = await _koreaInvestmentService.GetTokenAsync(appKey, appSecret);

            return Ok(new
            {
                AccessToken = result.access_token,
                ExpiresIn = result.expires_in,
                TokenType = result.token_type
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