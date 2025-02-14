using Microsoft.EntityFrameworkCore;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetByGoogleIdAsync(string googleId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<User> AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserDto> GetByEmailAsync(string email)
    {
        var user = await _context.Users
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.Email == email);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccountNumber = user.AccountNumber,
            KisAppKey = user.KisAppKey,
            KisAppSecret = user.KisAppSecret,
            KisToken = user.KisToken == null
                ? null
                : new KisTokenDto
                {
                    Id = user.KisToken.Id,
                    AccessToken = user.KisToken.AccessToken,
                    ExpiresIn = user.KisToken.ExpiresIn,
                    TokenType = user.KisToken.TokenType,
                }
        };
    }

    // public async Task<User> GetByEmailAsync(string email)
    // {
    //     // 쿼리 생성
    //     var query = _context.Users
    //         .Include(u => u.KisToken)
    //         .Where(u => u.Email == email);
    //
    //     // 실제 SQL 쿼리 확인
    //     var sql = query.ToQueryString();
    //     Console.WriteLine($"Generated SQL Query: {sql}");
    //
    //     // 쿼리 실행
    //     var user = await query.FirstOrDefaultAsync();
    //
    //     // 결과 로깅
    //     Console.WriteLine($"User found: {user?.Email}");
    //     Console.WriteLine($"KisToken loaded: {user?.KisToken != null}");
    //
    //     if (user?.KisToken == null)
    //     {
    //         // KisToken 직접 조회
    //         var kisToken = await _context.KisTokens
    //             .FirstOrDefaultAsync(t => t.UserId == user.Id);
    //         Console.WriteLine($"Direct KisToken query - Token exists: {kisToken != null}");
    //         if (kisToken != null)
    //         {
    //             Console.WriteLine($"Token Details - UserId: {kisToken.UserId}, ExpiresIn: {kisToken.ExpiresIn}");
    //         }
    //     }
    //
    //     return user;
    // }
}