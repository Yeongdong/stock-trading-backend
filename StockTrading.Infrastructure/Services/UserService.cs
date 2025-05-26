using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Common;
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

    public async Task<UserDto> GetOrCreateGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
    {
        _logger.LogInformation("Google 사용자 조회 또는 생성: {GoogleId}", payload.Subject);

        var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);

        if (user == null)
        {
            await using var transaction = await _dbContextWrapper.BeginTransactionAsync();
            
            _logger.LogInformation("새 Google 사용자 생성: {GoogleId}", payload.Subject);
            var newUser = new User
            {
                Email = payload.Email,
                Name = payload.Name,
                GoogleId = payload.Subject,
                CreatedAt = DateTime.UtcNow,
                Role = "User",
            };
            user = await _userRepository.AddAsync(newUser);
            
            await transaction.CommitAsync();
            _logger.LogInformation("새 사용자 생성 완료: {UserId}", user.Id);
        }

        return MapToDto(user);
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("이메일로 사용자 조회: {Email}", email);

        var user = await _userRepository.GetByEmailWithTokenAsync(email);
    
        if (user == null)
            throw new KeyNotFoundException($"이메일 {email}에 해당하는 사용자를 찾을 수 없습니다.");

        return MapToDto(user);
    }

    private UserDto MapToDto(User user)
    {
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
}