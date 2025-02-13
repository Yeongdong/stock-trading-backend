using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IKisService _kisService;
    private readonly IUserService _userService;
    private readonly ILogger<StockController> _logger;

    public StockController(IKisService kisService, IUserService userService, ILogger<StockController> logger)
    {
        _kisService = kisService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<StockBalance>> GetBalance()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByEmail(email);

        try
        {
            var balance = await _kisService.GetStockBalanceAsync(user);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "잔고 조회 중 오류 발생");
        }
    }
}