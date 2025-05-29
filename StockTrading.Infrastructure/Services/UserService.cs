using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;

namespace StockTrading.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IDbContextWrapper _dbContextWrapper;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger, IDbContextWrapper dbContextWrapper)
    {
        _userRepository = userRepository;
        _logger = logger;
        _dbContextWrapper = dbContextWrapper;
    }

    public async Task<UserInfo> GetOrCreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
    {
        if (payload?.Subject == null || payload.Email == null || payload.Name == null)
            throw new ArgumentException("유효하지 않은 Google 사용자 정보입니다.");

        _logger.LogInformation("Google 사용자 조회 또는 생성: {GoogleId}", payload.Subject);

        var existingUser = await _userRepository.GetByGoogleIdAsync(payload.Subject);

        if (existingUser != null)
            return ToUserDto(existingUser);

        return await CreateNewGoogleUserAsync(payload);
    }

    public async Task<UserInfo> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("이메일은 필수입니다.", nameof(email));

        _logger.LogInformation("이메일로 사용자 조회: {Email}", email);

        var user = await _userRepository.GetByEmailWithTokenAsync(email);
        if (user == null)
            throw new KeyNotFoundException($"이메일 {email}에 해당하는 사용자를 찾을 수 없습니다.");

        return ToUserDto(user);
    }

    private async Task<UserInfo> CreateNewGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
    {
        _logger.LogInformation("새 Google 사용자 생성: {GoogleId}", payload.Subject);

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var newUser = new User
        {
            Email = payload.Email,
            Name = payload.Name,
            GoogleId = payload.Subject,
            CreatedAt = DateTime.UtcNow,
            Role = "User",
        };

        var createdUser = await _userRepository.AddAsync(newUser);
        await transaction.CommitAsync();

        _logger.LogInformation("새 사용자 생성 완료: {UserId}", createdUser.Id);
        return ToUserDto(createdUser);
    }

    private UserInfo ToUserDto(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            AccountNumber = user.AccountNumber,
            KisAppKey = user.KisAppKey,
            KisAppSecret = user.KisAppSecret,
            WebSocketToken = user.WebSocketToken,
            KisToken = user.KisToken == null
                ? null
                : new KisTokenInfo
                {
                    Id = user.KisToken.Id,
                    AccessToken = user.KisToken.AccessToken,
                    ExpiresIn = user.KisToken.ExpiresIn,
                    TokenType = user.KisToken.TokenType,
                }
        };
    }
}