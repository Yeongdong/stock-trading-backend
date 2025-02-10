using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace stock_trading_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IKisService _kisService;
    private readonly IUserService _userService;
    private readonly DbContext _dbContext;

    public StockController(IKisService kisService, IUserService userService,
        DbContext dbContext)
    {
        _kisService = kisService;
        _userService = userService;
        _dbContext = dbContext;
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
        if (user == null)
        {
            return NotFound();
        }

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