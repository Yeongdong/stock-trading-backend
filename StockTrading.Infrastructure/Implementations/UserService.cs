using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Implementations;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetOrCreateGoogleUser(GoogleJsonWebSignature.Payload payload)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject);

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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> GetUserById(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User> GetUserByEmail(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }
    // getToken 후에 토큰을 User엔티티에 저장하는 로직 추가
}