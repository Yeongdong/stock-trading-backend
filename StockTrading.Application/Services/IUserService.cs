using Google.Apis.Auth;
using StockTrading.Application.DTOs.Common;

namespace StockTrading.Application.Services;

public interface IUserService
{
    Task<UserDto> GetOrCreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload);
    Task<UserDto> GetUserByEmailAsync(string email);
}