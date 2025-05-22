using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.Tests.Integration.Interfaces;
using StockTrading.Tests.Integration.Utilities;

namespace StockTrading.Tests.Integration.Implementations;

/// <summary>
/// 통합테스트용 인증 관리 구현체
/// JWT 토큰 생성 및 인증 시뮬레이션을 담당
/// </summary>
public class AuthenticationHelper : IAuthenticationHelper
{
    private readonly TestJwtTokenGenerator _tokenGenerator;
    private readonly IHttpClientHelper _httpClientHelper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationHelper> _logger;
    private UserDto _defaultTestUser;

    public AuthenticationHelper(
        TestJwtTokenGenerator tokenGenerator,
        IHttpClientHelper httpClientHelper,
        IConfiguration configuration,
        ILogger<AuthenticationHelper> logger)
    {
        _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
        _httpClientHelper = httpClientHelper ?? throw new ArgumentNullException(nameof(httpClientHelper));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeDefaultTestUser();
    }

    /// <summary>
    /// 기본 테스트 사용자로 JWT 토큰 생성
    /// </summary>
    public string GenerateTestToken()
    {
        try
        {
            var token = _tokenGenerator.GenerateToken();
            _logger.LogDebug("기본 테스트 사용자 토큰 생성 완료");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "기본 테스트 토큰 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 사용자로 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenForUser(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var token = _tokenGenerator.GenerateToken(user);
            _logger.LogDebug("사용자 {Email}의 토큰 생성 완료", user.Email);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 {Email}의 토큰 생성 중 오류 발생", user.Email);
            throw;
        }
    }

    /// <summary>
    /// 커스텀 클레임으로 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenWithClaims(params Claim[] claims)
    {
        if (claims == null || claims.Length == 0)
            throw new ArgumentException("클레임이 제공되지 않았습니다.", nameof(claims));

        try
        {
            var token = _tokenGenerator.GenerateTokenWithClaims(claims);
            _logger.LogDebug("커스텀 클레임 토큰 생성 완료: {ClaimCount}개 클레임", claims.Length);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "커스텀 클레임 토큰 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 역할(Role)로 JWT 토큰 생성
    /// </summary>
    public string GenerateTokenWithRole(string role, string email = null, string name = null)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("역할이 제공되지 않았습니다.", nameof(role));

        try
        {
            var token = _tokenGenerator.GenerateTokenWithRole(role, email, name);
            _logger.LogDebug("역할 {Role} 토큰 생성 완료", role);
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "역할 {Role} 토큰 생성 중 오류 발생", role);
            throw;
        }
    }

    /// <summary>
    /// 만료된 JWT 토큰 생성 (만료 테스트용)
    /// </summary>
    public string GenerateExpiredToken()
    {
        try
        {
            var token = _tokenGenerator.GenerateExpiredToken();
            _logger.LogDebug("만료된 토큰 생성 완료");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "만료된 토큰 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 잘못된 서명의 JWT 토큰 생성 (보안 테스트용)
    /// </summary>
    public string GenerateInvalidSignatureToken()
    {
        try
        {
            var token = _tokenGenerator.GenerateInvalidSignatureToken();
            _logger.LogDebug("잘못된 서명 토큰 생성 완료");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "잘못된 서명 토큰 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 인증된 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        try
        {
            var token = GenerateTestToken();
            return CreateAuthenticatedClientWithToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "인증된 클라이언트 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 사용자로 인증된 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateAuthenticatedClientForUser(UserDto user)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            var token = GenerateTokenForUser(user);
            var client = CreateAuthenticatedClientWithToken(token);
            _logger.LogDebug("사용자 {Email}의 인증된 클라이언트 생성 완료", user.Email);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 {Email}의 인증된 클라이언트 생성 중 오류 발생", user.Email);
            throw;
        }
    }

    /// <summary>
    /// 특정 토큰으로 인증된 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateAuthenticatedClientWithToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("토큰이 제공되지 않았습니다.", nameof(token));

        try
        {
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}"
            };

            var client = _httpClientHelper.CreateClientWithHeaders(headers);
            _logger.LogDebug("토큰 기반 인증된 클라이언트 생성 완료");
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "토큰 기반 인증된 클라이언트 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// Google OAuth 토큰 모킹
    /// </summary>
    public string GenerateMockGoogleCredential(string email, string name, string googleId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("이메일이 제공되지 않았습니다.", nameof(email));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("이름이 제공되지 않았습니다.", nameof(name));

        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("Google ID가 제공되지 않았습니다.", nameof(googleId));

        try
        {
            // Google JWT 토큰 구조를 모방한 클레임 생성
            var claims = new[]
            {
                new Claim("sub", googleId),
                new Claim("email", email),
                new Claim("name", name),
                new Claim("iss", "https://accounts.google.com"),
                new Claim("aud", _configuration["Authentication:Google:ClientId"] ?? "test-client-id"),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
            };

            var mockCredential = _tokenGenerator.GenerateTokenWithClaims(claims);
            _logger.LogDebug("Google 모의 자격증명 생성 완료: {Email}", email);
            return mockCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google 모의 자격증명 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 토큰 검증 (테스트 검증용)
    /// </summary>
    public ClaimsPrincipal ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("토큰이 제공되지 않았습니다.", nameof(token));

        try
        {
            var principal = _tokenGenerator.ValidateToken(token);
            _logger.LogDebug("토큰 검증 완료");
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "토큰 검증 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 현재 설정된 기본 테스트 사용자 정보 조회
    /// </summary>
    public UserDto GetDefaultTestUser()
    {
        return _defaultTestUser;
    }

    /// <summary>
    /// 기본 테스트 사용자 초기화
    /// </summary>
    private void InitializeDefaultTestUser()
    {
        try
        {
            var userConfig = _configuration.GetSection("TestData:User");

            _defaultTestUser = new UserDto
            {
                Id = userConfig.GetValue<int>("Id", 1),
                Email = userConfig["Email"] ?? "test@example.com",
                Name = userConfig["Name"] ?? "Test User",
                KisAppKey = userConfig["KisAppKey"] ?? "test_app_key",
                KisAppSecret = userConfig["KisAppSecret"] ?? "test_app_secret",
                AccountNumber = userConfig["AccountNumber"] ?? "1234567890",
                WebSocketToken = userConfig["WebSocketToken"] ?? "test_websocket_token",
                KisToken = new KisTokenDto
                {
                    Id = 1,
                    AccessToken = userConfig["KisAccessToken"] ?? "test_kis_access_token",
                    ExpiresIn = DateTime.UtcNow.AddHours(userConfig.GetValue<int>("TokenExpirationHours", 1)),
                    TokenType = userConfig["TokenType"] ?? "Bearer"
                }
            };

            _logger.LogDebug("기본 테스트 사용자 초기화 완료: {Email}", _defaultTestUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "기본 테스트 사용자 초기화 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 관리자 권한 인증된 클라이언트 생성
    /// </summary>
    public HttpClient CreateAdminAuthenticatedClient()
    {
        try
        {
            var token = _tokenGenerator.GenerateAdminToken();
            var client = CreateAuthenticatedClientWithToken(token);
            _logger.LogDebug("관리자 인증된 클라이언트 생성 완료");
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "관리자 인증된 클라이언트 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 권한들을 가진 인증된 클라이언트 생성
    /// </summary>
    public HttpClient CreateClientWithPermissions(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException("권한이 제공되지 않았습니다.", nameof(permissions));

        try
        {
            var token = _tokenGenerator.GenerateTokenWithPermissions(permissions);
            var client = CreateAuthenticatedClientWithToken(token);
            _logger.LogDebug("권한 기반 인증된 클라이언트 생성 완료: {PermissionCount}개 권한", permissions.Length);
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "권한 기반 인증된 클라이언트 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 쿠키 기반 인증된 클라이언트 생성 (쿠키 인증 테스트용)
    /// </summary>
    public HttpClient CreateCookieAuthenticatedClient()
    {
        try
        {
            var token = GenerateTestToken();
            var client = _httpClientHelper.CreateClient();

            // 쿠키 컨테이너 설정 (실제 구현에서는 CookieContainer 사용)
            client.DefaultRequestHeaders.Add("Cookie", $"auth_token={token}");

            _logger.LogDebug("쿠키 기반 인증된 클라이언트 생성 완료");
            return client;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "쿠키 기반 인증된 클라이언트 생성 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 토큰에서 사용자 정보 추출
    /// </summary>
    public UserDto ExtractUserFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("토큰이 제공되지 않았습니다.", nameof(token));

        try
        {
            var claims = _tokenGenerator.ExtractClaims(token);
            var claimsList = claims.ToList();

            var userDto = new UserDto
            {
                Email = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                Name = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            };

            var userIdClaim = claimsList.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                userDto.Id = userId;

            _logger.LogDebug("토큰에서 사용자 정보 추출 완료: {Email}", userDto.Email);
            return userDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "토큰에서 사용자 정보 추출 중 오류 발생");
            throw;
        }
    }
}