using Google.Apis.Auth;
using StockTrading.DataAccess.DTOs;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.DataAccess.Services.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateGoogleUser(GoogleJsonWebSignature.Payload payload);
    Task<User> GetUserById(int id);
    Task<UserDto> GetUserByEmail(string email);
}