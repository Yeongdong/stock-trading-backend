using System.Security.Claims;
using stock_trading_backend;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Infrastructure.Implementations;

public class GoogleAuthProvider : IGoogleAuthProvider
{
    public GoogleAuthProvider()
    {
    }

    public Task<GoogleUserInfo> GetUserInfoAsync(ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            throw new ArgumentNullException(nameof(principal), "ClaimsPrincipal 객체가 null입니다.");
        }
        
        var emailClaim = principal.FindFirst(ClaimTypes.Email).Value;
        var nameClaim = principal.FindFirst(ClaimTypes.Name).Value;
        
        if (emailClaim == null)
        {
            throw new InvalidOperationException("이메일 클레임을 찾을 수 없습니다.");
        }

        if (nameClaim == null)
        {
            throw new InvalidOperationException("이름 클레임을 찾을 수 없습니다.");
        }

        var googleUser = new GoogleUserInfo
        {
            Email = emailClaim,
            Name = nameClaim
        };

        return Task.FromResult(googleUser);
    }
}