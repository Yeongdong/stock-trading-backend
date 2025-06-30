using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Repositories;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;

namespace StockTrading.Infrastructure.Services.Auth;

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

    public async Task<UserInfo> CreateOrGetGoogleUserAsync(GoogleJsonWebSignature.Payload payload)
    {
        if (payload?.Subject == null || payload.Email == null || payload.Name == null)
            throw new ArgumentException("유효하지 않은 Google 사용자 정보입니다.");

        var existingUser = await _userRepository.GetByGoogleIdAsync(payload.Subject);
        if (existingUser != null)
            return ToUserDto(existingUser);

        return await CreateNewGoogleUserAsync(payload);
    }

    public async Task<UserInfo> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("이메일은 필수입니다.", nameof(email));

        var user = await _userRepository.GetByEmailWithTokenAsync(email);
        if (user == null)
            throw new KeyNotFoundException($"이메일 {email}에 해당하는 사용자를 찾을 수 없습니다.");

        return ToUserDto(user);
    }

    public async Task DeleteAccountAsync(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("유효하지 않은 사용자 ID입니다.", nameof(userId));

        _logger.LogInformation("회원 탈퇴 시작: 사용자 ID {UserId}", userId);

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"사용자 ID {userId}를 찾을 수 없습니다.");

        await _userRepository.DeleteAsync(user.Id);
        await transaction.CommitAsync();

        _logger.LogInformation("회원 탈퇴 완료: 사용자 ID {UserId}, 이메일 {Email}", userId, user.Email);
    }

    public async Task<AccountBalance> GetAccountBalanceWithDailyProfitAsync(UserInfo user,
        ITradingService tradingService)
    {
        // 기본 잔고 조회
        var balance = await tradingService.GetStockBalanceAsync(user);

        // 당일손익 계산
        var dailyProfitLoss = await CalculateDailyProfitLossAsync(user.Id, balance.Summary);

        return new AccountBalance
        {
            Positions = balance.Positions,
            Summary = balance.Summary,
            DailyProfitLossAmount = dailyProfitLoss.Amount,
            DailyProfitLossRate = dailyProfitLoss.Rate
        };
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
            Role = UserRole.User,
        };

        var createdUser = await _userRepository.AddAsync(newUser);
        await transaction.CommitAsync();

        return ToUserDto(createdUser);
    }

    private UserInfo ToUserDto(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt,
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

    private async Task<(decimal Amount, decimal Rate)> CalculateDailyProfitLossAsync(
        int userId, KisAccountSummaryResponse summary)
    {
        var currentTotalAmount = decimal.Parse(summary.TotalEvaluation);

        // 사용자의 전일 총평가금액 조회
        var user = await _userRepository.GetByIdAsync(userId);
        var previousDayAmount = user?.PreviousDayTotalAmount;

        if (previousDayAmount == null || previousDayAmount == 0)
        {
            // 전일 데이터가 없으면 당일손익은 0
            return (0, 0);
        }

        // 당일손익 = 현재 총평가금액 - 전일 총평가금액
        var dailyProfitAmount = currentTotalAmount - previousDayAmount.Value;

        // 당일손익률 = (당일손익 / 전일 총평가금액) * 100
        var dailyProfitRate = previousDayAmount.Value != 0
            ? (dailyProfitAmount / previousDayAmount.Value) * 100
            : 0;

        return (dailyProfitAmount, dailyProfitRate);
    }

    public async Task UpdatePreviousDayTotalAmountAsync(UserInfo user, decimal currentTotalAmount)
    {
        await _userRepository.UpdatePreviousDayTotalAmountAsync(user.Id, currentTotalAmount);
    }
}