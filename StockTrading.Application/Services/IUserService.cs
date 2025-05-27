using Google.Apis.Auth;
using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.Services;

public interface IUserService
{
    Task<UserInfo> GetOrCreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload);
    Task<UserInfo> GetUserByEmailAsync(string email);
}