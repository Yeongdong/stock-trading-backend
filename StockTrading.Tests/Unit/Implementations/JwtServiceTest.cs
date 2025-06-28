using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Exceptions.Authentication;
using StockTrading.Domain.Settings.Infrastructure;
using StockTrading.Infrastructure.Services.Auth;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(JwtService))]
public class JwtServiceTest
{
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IOptions<JwtSettings>> _mockOptions;
    private readonly Mock<ILogger<JwtService>> _mockLogger;
    private readonly JwtService _jwtService;
    private readonly UserInfo _testUser;

    public JwtServiceTest()
    {
        _jwtSettings = new JwtSettings
        {
            Key = "super_secure_test_key_with_at_least_32_chars_length",
            Issuer = "test_issuer",
            Audience = "test_audience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        _mockOptions = new Mock<IOptions<JwtSettings>>();
        _mockOptions.Setup(o => o.Value).Returns(_jwtSettings);

        _mockLogger = new Mock<ILogger<JwtService>>();

        _jwtService = new JwtService(_mockOptions.Object, _mockLogger.Object);

        _testUser = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        var token = _jwtService.GenerateAccessToken(_testUser);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == _testUser.Email);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == _testUser.Name);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Equal(_jwtSettings.Audience, jwtToken.Audiences.FirstOrDefault());

        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        Assert.True(Math.Abs((expectedExpiration - jwtToken.ValidTo).TotalMinutes) < 5,
            "토큰 만료 시간이 예상 범위 내에 있어야 합니다.");
    }
    
    [Fact]
    public void GenerateRefreshToken_ShouldReturnValidTokenAndExpiryDate()
    {
        var (refreshToken, expiryDate) = _jwtService.GenerateRefreshToken();

        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
        
        // Base64 형식 확인 (디코딩 가능한지)
        var base64Regex = new System.Text.RegularExpressions.Regex(
            @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        Assert.Matches(base64Regex, refreshToken);
        
        // 길이 확인 (32바이트 -> Base64로 인코딩 후 약 44자)
        Assert.True(refreshToken.Length >= 42 && refreshToken.Length <= 46, 
            "리프레시 토큰은 Base64로 인코딩된 32바이트여야 합니다.");
        
        // 만료 시간 확인 - 리프레시 토큰은 RefreshTokenExpirationDays 사용
        var expectedExpiration = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        var timeDifference = Math.Abs((expectedExpiration - expiryDate).TotalHours);
        Assert.True(timeDifference < 1, 
            $"토큰 만료 시간이 예상 범위 내에 있어야 합니다. 예상: {expectedExpiration:yyyy-MM-dd HH:mm:ss}, 실제: {expiryDate:yyyy-MM-dd HH:mm:ss}, 차이: {timeDifference}시간");
    }
    
    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        var token = _jwtService.GenerateAccessToken(_testUser);

        var principal = _jwtService.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
            
        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        var nameClaim = principal.FindFirst(ClaimTypes.Name);
            
        Assert.NotNull(emailClaim);
        Assert.NotNull(nameClaim);
        Assert.Equal(_testUser.Email, emailClaim.Value);
        Assert.Equal(_testUser.Name, nameClaim.Value);
    }
    
    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldThrowTokenValidationException()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, _testUser.Email),
            new Claim(ClaimTypes.Name, _testUser.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
            
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-5), // 5분 전에 만료
            signingCredentials: credentials);

        var expiredToken = new JwtSecurityTokenHandler().WriteToken(token);

        var exception = Assert.Throws<TokenValidationException>(() => 
            _jwtService.ValidateToken(expiredToken));
            
        Assert.Equal("토큰이 만료되었습니다.", exception.Message);
    }
    
    [Fact]
    public void ValidateToken_WithInvalidSignature_ShouldThrowTokenValidationException()
    {
        var token = _jwtService.GenerateAccessToken(_testUser);
    
        var parts = token.Split('.');
        if (parts.Length == 3)
        {
            // 서명 부분을 완전히 다른 값으로 교체
            parts[2] = "invalid_signature_that_will_definitely_fail_validation";
            var invalidToken = string.Join(".", parts);

            var exception = Assert.Throws<TokenValidationException>(() => 
                _jwtService.ValidateToken(invalidToken));
            
            Assert.Equal("토큰 서명이 유효하지 않습니다.", exception.Message);
        }
        else
        {
            // JWT 형태가 올바르지 않은 경우를 위한 fallback
            var invalidToken = "invalid.jwt.token";
        
            var exception = Assert.Throws<TokenValidationException>(() => 
                _jwtService.ValidateToken(invalidToken));
            
            Assert.Equal("토큰 검증 실패", exception.Message);
        }
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ShouldThrowTokenValidationException()
    {
        var invalidToken = "this.is.not.a.valid.jwt.token.format";

        var exception = Assert.Throws<TokenValidationException>(() => 
            _jwtService.ValidateToken(invalidToken));
        
        Assert.Equal("토큰 검증 실패", exception.Message);
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldThrowTokenValidationException()
    {
        var exception = Assert.Throws<TokenValidationException>(() => 
            _jwtService.ValidateToken(""));
        
        Assert.Equal("토큰 검증 실패", exception.Message);
    }

    [Fact]
    public void ValidateToken_WithNullToken_ShouldThrowTokenValidationException()
    {
        var exception = Assert.Throws<TokenValidationException>(() => 
            _jwtService.ValidateToken(null));
        
        Assert.Equal("토큰 검증 실패", exception.Message);
    }
    
    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldThrowTokenValidationException()
    {
        var invalidToken = "invalid_token_format";

        var exception = Assert.Throws<TokenValidationException>(() => 
            _jwtService.ValidateToken(invalidToken));
            
        Assert.Equal("토큰 검증 실패", exception.Message);
    }
}