using Google.Apis.Auth;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Users.Services;

public interface IUserService
{
    Task<UserInfo> CreateOrGetGoogleUserAsync(GoogleJsonWebSignature.Payload payload);
    Task<UserInfo> GetUserByEmailAsync(string email);
    Task DeleteAccountAsync(int userId);
    Task<AccountBalance> GetAccountBalanceWithDailyProfitAsync(UserInfo user, ITradingService tradingService);
}