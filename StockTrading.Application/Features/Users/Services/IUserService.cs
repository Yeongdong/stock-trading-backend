using Google.Apis.Auth;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Users.Services;

public interface IUserService
{
    Task<UserInfo> CreateOrGetGoogleUserAsync(GoogleJsonWebSignature.Payload payload);
    Task<UserInfo> GetUserByEmailAsync(string email);
}