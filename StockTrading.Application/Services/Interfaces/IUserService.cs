using Google.Apis.Auth;
using StockTrading.DataAccess.DTOs;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> GetOrCreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload);
    Task<UserDto> GetUserByEmailAsync(string email);
}