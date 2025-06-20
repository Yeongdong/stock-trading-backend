using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Exceptions.Authentication;
using StockTrading.Domain.Settings.Infrastructure;

namespace StockTrading.Infrastructure.Services.Auth;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    public string GenerateToken(UserInfo user)
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

    public (string token, DateTime expiryDate) GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        var refreshToken = Convert.ToBase64String(randomNumber);
        var expiryDate = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return (refreshToken, expiryDate);
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
        catch (SecurityTokenMalformedException)
        {
            _logger.LogWarning("형식이 잘못된 토큰");
            throw new TokenValidationException("토큰 검증 실패");
        }
        catch (SecurityTokenValidationException)
        {
            _logger.LogWarning("토큰 검증 실패");
            throw new TokenValidationException("토큰 검증 실패");
        }
        catch (ArgumentNullException)
        {
            _logger.LogWarning("토큰이 null입니다.");
            throw new TokenValidationException("토큰 검증 실패");
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("잘못된 형식의 토큰");
            throw new TokenValidationException("토큰 검증 실패");
        }
    }
}