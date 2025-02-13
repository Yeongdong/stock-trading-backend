using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User> GetOrCreateGoogleUser(GoogleJsonWebSignature.Payload payload)
    {
        var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);

        if (user == null)
        {
            var newUser = new User
            {
                Email = payload.Email,
                Name = payload.Name,
                GoogleId = payload.Subject,
                CreatedAt = DateTime.UtcNow,
                Role = "User",
            };
            user = await _userRepository.AddAsync(newUser);
        }

        return user;
    }

    public async Task<User> GetUserById(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<UserDto> GetUserByEmail(string email)
    {
        var userDto = await _userRepository.GetByEmailAsync(email);

        return userDto;
    }
}