using System.Security.Claims;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Services;

namespace StockTrading.API.Services;

public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserService _userService;

    public UserContextService(IHttpContextAccessor httpContextAccessor, IUserService userService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userService = userService;
    }

    public async Task<UserInfo> GetCurrentUserAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        var email = context?.User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException("사용자 인증 정보가 유효하지 않습니다.");

        var user = await _userService.GetUserByEmailAsync(email);

        if (user == null)
            throw new KeyNotFoundException("사용자 정보를 찾을 수 없습니다.");

        return user;
    }
}