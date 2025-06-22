using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.Features.Auth.DTOs;
using StockTrading.Application.Features.Auth.Repositories;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Entities.Auth;
using StockTrading.Infrastructure.Validator.Interfaces;

namespace StockTrading.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly IUserService _userService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IGoogleAuthValidator _googleAuthValidator;
    private readonly IConfiguration _configuration;
    private readonly ICookieService _cookieService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IJwtService jwtService,
        IUserService userService,
        IRefreshTokenRepository refreshTokenRepository,
        IGoogleAuthValidator googleAuthValidator,
        IConfiguration configuration,
        ICookieService cookieService,
        ILogger<AuthService> logger)
    {
        _jwtService = jwtService;
        _userService = userService;
        _refreshTokenRepository = refreshTokenRepository;
        _googleAuthValidator = googleAuthValidator;
        _configuration = configuration;
        _cookieService = cookieService;
        _logger = logger;
    }

    public async Task<LoginResponse> GoogleLoginAsync(string credential)
    {
        var payload = await _googleAuthValidator.ValidateAsync(credential, _configuration["Authentication:Google:ClientId"]);
        var user = await _userService.CreateOrGetGoogleUserAsync(payload);

        // 기존 토큰들 폐기
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id);

        // 새 토큰 생성
        var accessToken = _jwtService.GenerateAccessToken(user);
        var (refreshToken, refreshExpiry) = _jwtService.GenerateRefreshToken();

        // Refresh Token 저장
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            ExpiresAt = refreshExpiry,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        // Refresh Token을 httpOnly 쿠키로 설정
        _cookieService.SetRefreshTokenCookie(refreshToken);

        return new LoginResponse
        {
            AccessToken = accessToken,
            User = user,
            Message = "로그인 성공"
        };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync()
    {
        // 쿠키에서 Refresh Token 추출
        var refreshToken = _cookieService.GetRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new AuthenticationException("Refresh Token이 없습니다.");

        var refreshTokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        
        if (refreshTokenEntity is not { IsActive: true })
            throw new AuthenticationException("유효하지 않은 Refresh Token입니다.");

        // 기존 토큰 폐기 (RTR)
        refreshTokenEntity.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(refreshTokenEntity);

        // 새 토큰 생성
        var user = refreshTokenEntity.User;
        var userDto = ToUserDto(user);
        var newAccessToken = _jwtService.GenerateAccessToken(userDto);
        var (newRefreshToken, newRefreshExpiry) = _jwtService.GenerateRefreshToken();

        // 새 Refresh Token 저장
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshToken,
            ExpiresAt = newRefreshExpiry,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

        // 새 Refresh Token을 쿠키로 설정 (RTR)
        _cookieService.SetRefreshTokenCookie(newRefreshToken);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            Message = "토큰 갱신 성공"
        };
    }

    public async Task LogoutAsync(int userId)
    {
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        
        _cookieService.DeleteAuthCookie();
        _cookieService.DeleteRefreshTokenCookie();
        
        _logger.LogInformation("로그아웃: 사용자 {UserId}", userId);
    }

    private UserInfo ToUserDto(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            KisAppKey = user.KisAppKey,
            KisAppSecret = user.KisAppSecret,
            AccountNumber = user.AccountNumber,
            WebSocketToken = user.WebSocketToken
        };
    }
}