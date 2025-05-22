using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StockTrading.DataAccess.DTOs;

namespace StockTrading.Tests.Integration.Utilities;

/// <summary>
/// 테스트용 JWT 토큰 생성 유틸리티
/// 다양한 시나리오의 JWT 토큰을 생성하여 인증 테스트 지원
/// </summary>
public class TestJwtTokenGenerator
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _defaultExpirationMinutes;

    public TestJwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tokenHandler = new JwtSecurityTokenHandler();

        var jwtSection = _configuration.GetSection("JwtSettings");
        _secretKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key가 설정되지 않았습니다.");
        _issuer = jwtSection["Issuer"] ?? "TestIssuer";
        _audience = jwtSection["Audience"] ?? "TestAudience";
        _defaultExpirationMinutes = jwtSection.GetValue("AccessTokenExpirationMinutes", 30);
    }

    /// <summary>
    /// 기본 테스트 사용자로 JWT 토큰 생성
    /// </summary>
    public string GenerateToken()
    {
        var testUserConfig = _configuration.GetSection("TestData:User");
        var email = testUserConfig["Email"] ?? "test@example.com";
        var name = testUserConfig["Name"] ?? "Test User";
        var role = testUserConfig["Role"] ?? "User";

        return GenerateToken(email, name, role);
    }

    /// <summary>
    /// 특정 사용자 정보로 JWT 토큰 생성
    /// </summary>
    public string GenerateToken(UserDto user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        return GenerateToken(user.Email, user.Name, "User", user.Id.ToString());
    }

    /// <summary>
    /// 사용자 정보를 직접 지정하여 JWT 토큰 생성
    /// </summary>
    public string GenerateToken(string email, string name, string role = "User", string userId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // UserId가 제공된 경우 추가
        if (!string.IsNullOrEmpty(userId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        }

        return GenerateTokenWithClaims(claims.ToArray());
    }

    /// <summary>
    /// 커스텀 클레임으로 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenWithClaims(params Claim[] claims)
    {
        return GenerateTokenWithClaims(DateTime.UtcNow.AddMinutes(_defaultExpirationMinutes), claims);
    }

    /// <summary>
    /// 특정 만료 시간과 클레임으로 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenWithClaims(DateTime expirationTime, params Claim[] claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expirationTime,
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 특정 역할로 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenWithRole(string role, string email = null, string name = null)
    {
        email ??= "test@example.com";
        name ??= "Test User";

        return GenerateToken(email, name, role);
    }

    /// <summary>
    /// 만료된 JWT 토큰 생성 (만료 테스트용)
    /// </summary>
    public string GenerateExpiredToken()
    {
        var expiredTime = DateTime.UtcNow.AddMinutes(-10); // 10분 전에 만료

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return GenerateTokenWithClaims(expiredTime, claims);
    }

    /// <summary>
    /// 잘못된 서명의 JWT 토큰 생성 (보안 테스트용)
    /// </summary>
    public string GenerateInvalidSignatureToken()
    {
        // 다른 키로 서명하여 잘못된 서명 토큰 생성
        var invalidKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("invalid_secret_key_for_testing_purposes_only"));
        var invalidCredentials = new SigningCredentials(invalidKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_defaultExpirationMinutes),
            signingCredentials: invalidCredentials
        );

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 잘못된 형식의 JWT 토큰 생성 (형식 테스트용)
    /// </summary>
    public string GenerateMalformedToken()
    {
        return "invalid.jwt.token.format";
    }

    /// <summary>
    /// 관리자 권한 JWT 토큰 생성
    /// </summary>
    public string GenerateAdminToken()
    {
        return GenerateTokenWithRole("Admin", "admin@example.com", "Admin User");
    }

    /// <summary>
    /// 특정 권한들을 가진 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenWithPermissions(params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, "test@example.com"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Role, "User"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // 권한들을 클레임으로 추가
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        return GenerateTokenWithClaims(claims.ToArray());
    }

    /// <summary>
    /// 토큰 검증 (테스트 검증용)
    /// </summary>
    public ClaimsPrincipal ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        var principal = _tokenHandler.ValidateToken(token, validationParameters, out _);
        return principal;
    }

    /// <summary>
    /// 토큰에서 클레임 추출
    /// </summary>
    public IEnumerable<Claim> ExtractClaims(string token)
    {
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        return jwtToken.Claims;
    }

    /// <summary>
    /// 토큰 만료 시간 확인
    /// </summary>
    public DateTime GetTokenExpiration(string token)
    {
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        return jwtToken.ValidTo;
    }
}