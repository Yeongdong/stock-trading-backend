using Google.Apis.Auth;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> GetOrCreateGoogleUser(GoogleJsonWebSignature.Payload payload)
    {
        var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);

        if (user == null)
        {
            user = new User
            {
                Email = payload.Email,
                Name = payload.Name,
                GoogleId = payload.Subject,
                CreatedAt = DateTime.UtcNow,
                Role = "User",
            };

            user = await _userRepository.AddAsync(user);
        }

        return user;
    }

    public async Task<User> GetUserById(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User> GetUserByEmail(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }
}