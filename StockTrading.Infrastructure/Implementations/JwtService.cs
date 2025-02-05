using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StockTradingBackend.DataAccess.Entities;
using StockTradingBackend.DataAccess.Exceptions.Authentication;
using StockTradingBackend.DataAccess.Interfaces;
using StockTradingBackend.DataAccess.Settings;

namespace StockTrading.Infrastructure.Implementations;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        try
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "액세스 토큰 생성 중 오류 발생");
            throw new TokenValidationException("토큰 생성 실패", ex);
        }
    }

    public (string token, DateTime expiryDate) GenerateRefreshToken()
    {
        try
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            var refreshToken = Convert.ToBase64String(randomNumber);
            var expiryDate = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            return (refreshToken, expiryDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "리프레시 토큰 생성 중 오류 발생");
            throw new TokenValidationException("리프레시 토큰 생성 실패", ex);
        }
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, parameters, out SecurityToken validatedToken);

            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("만료된 토큰");
            throw new TokenValidationException("토큰이 만료되었습니다.");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("잘못된 서명의 토큰");
            throw new TokenValidationException("토큰 서명이 유효하지 않습니다.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "토큰 검증 중 오류 발생");
            throw new TokenValidationException("토큰 검증 실패", ex);
        }
    }
}