using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using StockTrading.DataAccess.DTOs;
using StockTrading.Infrastructure.Implementations;
using StockTradingBackend.DataAccess.Exceptions.Authentication;
using StockTradingBackend.DataAccess.Settings;
using static System.Text.Encoding;
using static Microsoft.IdentityModel.Tokens.SecurityAlgorithms;

namespace StockTrading.Tests.Integration.Services;

public class JwtAuthenticationIntegrationTest
{
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<IOptions<JwtSettings>> _mockOptions;
    private readonly ILogger<JwtService> _logger;
    private readonly JwtService _jwtService;
    private readonly UserDto _testUser;

    public JwtAuthenticationIntegrationTest()
    {
        // 테스트용 JWT 설정
        _jwtSettings = new JwtSettings
        {
            Key = "super_secure_test_key_with_at_least_32_chars_for_hmacsha256",
            Issuer = "test_issuer",
            Audience = "test_audience",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        };

        _mockOptions = new Mock<IOptions<JwtSettings>>();
        _mockOptions.Setup(o => o.Value).Returns(_jwtSettings);
        _logger = new NullLogger<JwtService>();
        _jwtService = new JwtService(_mockOptions.Object, _logger);

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "test_app_key",
            KisAppSecret = "test_app_secret"
        };
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsValidJwtToken()
    {
        var token = _jwtService.GenerateToken(_testUser);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // 토큰 구조 검증
        var tokenHandler = new JwtSecurityTokenHandler();
        Assert.True(tokenHandler.CanReadToken(token));

        var jwtToken = tokenHandler.ReadJwtToken(token);

        // 클레임 검증
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Email && c.Value == _testUser.Email);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Name && c.Value == _testUser.Name);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);

        // 발급자 및 수신자 검증
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
        Assert.Equal(_jwtSettings.Audience, jwtToken.Audiences.FirstOrDefault());

        // 만료 시간 검증
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var timeDifference = Math.Abs((expectedExpiration - jwtToken.ValidTo).TotalMinutes);
        Assert.True(timeDifference < 5, "토큰 만료 시간이 예상 범위 내에 있어야 합니다.");
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
    {
        var token = _jwtService.GenerateToken(_testUser);

        var principal = _jwtService.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.True(principal.Identity.IsAuthenticated);

        // 클레임 검증
        var emailClaim = principal.FindFirst(ClaimTypes.Email);
        var nameClaim = principal.FindFirst(ClaimTypes.Name);

        Assert.NotNull(emailClaim);
        Assert.NotNull(nameClaim);
        Assert.Equal(_testUser.Email, emailClaim.Value);
        Assert.Equal(_testUser.Name, nameClaim.Value);
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ThrowsTokenValidationException()
    {
        var expiredToken = CreateExpiredToken();

        var exception = Assert.Throws<TokenValidationException>(() =>
            _jwtService.ValidateToken(expiredToken));

        Assert.Contains("만료", exception.Message.ToLower());
    }

    [Fact]
    public void ValidateToken_InvalidSignature_ThrowsTokenValidationException()
    {
        var token = _jwtService.GenerateToken(_testUser);
        var tamperedToken = TamperWithToken(token);

        var exception = Assert.Throws<TokenValidationException>(() =>
            _jwtService.ValidateToken(tamperedToken));

        Assert.Contains("유효하지 않", exception.Message.ToLower());
    }
    
    [Fact]
    public void GenerateRefreshToken_ReturnsTokenAndExpiryDate()
    {
        var (refreshToken, expiryDate) = _jwtService.GenerateRefreshToken();

        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken);
            
        // Base64 형식 확인
        var base64Regex = new System.Text.RegularExpressions.Regex(
            @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
        Assert.Matches(base64Regex, refreshToken);
            
        // 토큰 길이 확인 (32바이트 -> Base64로 인코딩 후 약 44자)
        Assert.True(refreshToken.Length >= 42 && refreshToken.Length <= 46);
            
        // 만료 시간 확인
        var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var timeDifference = Math.Abs((expectedExpiration - expiryDate).TotalMinutes);
        Assert.True(timeDifference < 5);
    }
    
    [Fact]
    public void ValidateToken_NullOrEmptyToken_ThrowsTokenValidationException()
    {
        Assert.Throws<TokenValidationException>(() => _jwtService.ValidateToken(null));
        Assert.Throws<TokenValidationException>(() => _jwtService.ValidateToken(string.Empty));
    }

    [Fact]
    public void ValidateToken_MalformedToken_ThrowsTokenValidationException()
    {
        var malformedToken = "not.a.valid.jwt.token";

        Assert.Throws<TokenValidationException>(() => _jwtService.ValidateToken(malformedToken));
    }

    #region Helper Methods

    private string CreateExpiredToken()
    {
        var key = new SymmetricSecurityKey(UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, _testUser.Email),
            new Claim(ClaimTypes.Name, _testUser.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(-5), // 5분 전에 만료
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string TamperWithToken(string token)
    {
        // 토큰의 마지막 문자를 변경하여 서명을 손상
        if (string.IsNullOrEmpty(token) || token.Length < 2)
            return token;

        var lastChar = token[token.Length - 1];
        var tampered = token.Substring(0, token.Length - 1) +
                       (lastChar == 'A' ? 'B' : 'A');

        return tampered;
    }

    #endregion
}