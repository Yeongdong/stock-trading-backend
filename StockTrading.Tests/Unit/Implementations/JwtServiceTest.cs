using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using StockTrading.DataAccess.DTOs;
using StockTrading.Infrastructure.Implementations;
using StockTradingBackend.DataAccess.Exceptions.Authentication;
using StockTradingBackend.DataAccess.Settings;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(JwtService))]
public class JwtServiceTest
{
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IOptions<JwtSettings>> _mockOptions;
    private readonly Mock<ILogger<JwtService>> _mockLogger;
    private readonly JwtService _jwtService;
    private readonly UserDto _testUser;

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

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        var token = _jwtService.GenerateToken(_testUser);

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
        var fiveMinutesWindow = TimeSpan.FromMinutes(5);
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
            
        // 만료 시간 확인
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var fiveMinutesWindow = TimeSpan.FromMinutes(5);
        Assert.True(Math.Abs((expectedExpiration - expiryDate).TotalMinutes) < 5,
            "토큰 만료 시간이 예상 범위 내에 있어야 합니다.");
    }
    
    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        var token = _jwtService.GenerateToken(_testUser);

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
        var token = _jwtService.GenerateToken(_testUser);
            
        // 마지막 문자를 변경하여 서명을 손상
        var invalidToken = token.Substring(0, token.Length - 1) + 
                           (token[token.Length - 1] == 'A' ? 'B' : 'A');

        var exception = Assert.Throws<TokenValidationException>(() => 
            _jwtService.ValidateToken(invalidToken));
            
        Assert.Equal("토큰 서명이 유효하지 않습니다.", exception.Message);
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