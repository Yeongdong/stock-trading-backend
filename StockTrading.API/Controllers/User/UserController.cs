using Microsoft.AspNetCore.Mvc;
using StockTrading.API.Services;
using StockTrading.Application.Features.Users.Services;

namespace StockTrading.API.Controllers.User;

[Route("api/[controller]")]
public class UserController : BaseController
{
    private readonly IUserService _userService;

    public UserController(IUserContextService userContextService, IUserService userService)
        : base(userContextService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        var user = await GetCurrentUserAsync();
        return Ok(user);
    }
    
    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var user = await GetCurrentUserAsync();
        await _userService.DeleteAccountAsync(user.Id);

        return Ok(new { Message = "회원 탈퇴가 완료되었습니다." });
    }
}