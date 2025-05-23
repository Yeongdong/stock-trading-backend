using Google.Apis.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using stock_trading_backend.controllers;
using stock_trading_backend.DTOs;
using stock_trading_backend.Validator.Interfaces;

namespace StockTrading.Tests.Integration.Controllers;

/// <summary>
/// 인증 관련 기능 (Google 로그인, 토큰 검증, 로그아웃) 테스트
/// </summary>
public class AuthControllerIntegrationTest : ControllerTestBase<AuthController>
{
    private readonly Mock<IGoogleAuthValidator> _mockGoogleAuthValidator;

    public AuthControllerIntegrationTest(IntegrationTestWebApplicationFactory factory) : base(factory)
    {
        _mockGoogleAuthValidator = new Mock<IGoogleAuthValidator>();

        SetupMockServices();
        SetupGoogleAuthValidatorMock();
    }

    #region 설정 및 헬퍼 메서드

    /// <summary>
    /// Mock 서비스를 DI 컨테이너에 등록
    /// </summary>
    private void SetupMockServices()
    {
        // 테스트용 Mock 서비스로 교체
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_mockGoogleAuthValidator.Object);
    }

    /// <summary>
    /// Google 인증 검증기 모킹 설정
    /// </summary>
    private void SetupGoogleAuthValidatorMock()
    {
        var validPayload = new GoogleJsonWebSignature.Payload
        {
            Subject = "test_google_id_123",
            Email = "test@example.com",
            Name = "Test User",
            Issuer = "https://accounts.google.com",
            Audience = "test_google_client_id",
            ExpirationTimeSeconds = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            IssuedAtTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _mockGoogleAuthValidator
            .Setup(v => v.ValidateAsync(It.Is<string>(s => s.StartsWith("valid_")), It.IsAny<string>()))
            .ReturnsAsync(validPayload);

        _mockGoogleAuthValidator
            .Setup(v => v.ValidateAsync(It.Is<string>(s => s.StartsWith("invalid_")), It.IsAny<string>()))
            .ThrowsAsync(new InvalidJwtException("Invalid JWT token"));
    }

    /// <summary>
    /// 유효한 Google JWT 토큰 형식 생성
    /// </summary>
    private string CreateValidGoogleJwtToken()
    {
        var header = "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE2NzAyNjk2MzEifQ";
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(@"{
            ""sub"": ""test_google_id_123"",
            ""email"": ""test@example.com"",
            ""name"": ""Test User"",
            ""iss"": ""https://accounts.google.com"",
            ""aud"": ""test_google_client_id"",
            ""exp"": " + DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() + @",
            ""iat"": " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + @"
        }"));
        var signature = "test_signature_mock";

        return $"{header}.{payload}.{signature}";
    }

    /// <summary>
    /// 유효한 Google 로그인 요청 생성
    /// </summary>
    private GoogleLoginRequest CreateValidGoogleLoginRequest()
    {
        return new GoogleLoginRequest
        {
            Credential = CreateValidGoogleJwtToken()
        };
    }

    /// <summary>
    /// 잘못된 Google 로그인 요청 생성
    /// </summary>
    private static GoogleLoginRequest CreateInvalidGoogleLoginRequest()
    {
        return new GoogleLoginRequest
        {
            Credential = "invalid_google_jwt_token"
        };
    }

    /// <summary>
    /// 응답에서 쿠키 값 추출
    /// </summary>
    private static string ExtractCookieFromResponse(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        var targetCookie = cookies.FirstOrDefault(c => c.StartsWith($"{cookieName}="));

        var cookieValue = targetCookie?.Split(';')[0].Split('=')[1];
        return cookieValue;
    }

    /// <summary>
    /// 쿠키가 설정된 클라이언트 생성
    /// </summary>
    private HttpClient CreateClientWithCookie(string cookieName, string cookieValue)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"{cookieName}={cookieValue}");
        return client;
    }

    #endregion

    #region Google 로그인 기능 테스트

    [Fact]
    public async Task GoogleLogin_WithValidCredentials_ShouldReturnSuccessAndSetCookie()
    {
        await RunTestWithLoggingAsync(nameof(GoogleLogin_WithValidCredentials_ShouldReturnSuccessAndSetCookie),
            async () =>
            {
                // Arrange
                var loginRequest = CreateValidGoogleLoginRequest();

                // Act
                var response = await PostAsync(loginRequest, "google");
                var responseContent = await response.Content.ReadAsStringAsync();

                // Assert
                await AssertSuccessResponseAsync(response);

                // 응답 데이터 검증
                var loginResponse = await HttpClientHelper.DeserializeResponseAsync<GoogleLoginResponse>(response);
                Assert.NotNull(loginResponse);
                Assert.NotNull(loginResponse.User);
                Assert.Equal("test@example.com", loginResponse.User.Email);
                Assert.Equal("Test User", loginResponse.User.Name);

                // 쿠키 설정 확인
                var authCookie = ExtractCookieFromResponse(response, "auth_token");
                Assert.NotNull(authCookie);
                Assert.NotEmpty(authCookie);
            });
    }

    [Fact]
    public async Task GoogleLogin_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(GoogleLogin_WithInvalidCredentials_ShouldReturnBadRequest),
            async () =>
            {
                // Arrange
                var invalidRequest = CreateInvalidGoogleLoginRequest();

                // Act
                var response = await PostAsync(invalidRequest, "google");

                // Assert
                await AssertBadRequestAsync(response);
            });
    }

    [Fact]
    public async Task GoogleLogin_WithEmptyCredentials_ShouldReturnBadRequest()
    {
        await RunTestWithLoggingAsync(nameof(GoogleLogin_WithEmptyCredentials_ShouldReturnBadRequest),
            async () =>
            {
                // Arrange
                var emptyRequest = new GoogleLoginRequest { Credential = "" };

                // Act
                var response = await PostAsync(emptyRequest, "google");

                // Assert
                await AssertBadRequestAsync(response);
            });
    }

    #endregion

    #region 인증 확인(CheckAuth) 기능 테스트

    [Fact]
    public async Task CheckAuth_WithValidCookie_ShouldReturnAuthenticatedUser()
    {
        await RunTestWithLoggingAsync(nameof(CheckAuth_WithValidCookie_ShouldReturnAuthenticatedUser),
            async () =>
            {
                // Arrange
                var loginRequest = CreateValidGoogleLoginRequest();
                var loginResponse = await PostAsync(loginRequest, "google");
                await AssertSuccessResponseAsync(loginResponse);

                var authCookie = ExtractCookieFromResponse(loginResponse, "auth_token");
                Assert.NotNull(authCookie);

                var clientWithCookie = CreateClientWithCookie("auth_token", authCookie);

                // Act
                var checkResponse = await HttpClientHelper.GetAsync("/api/auth/check", clientWithCookie);

                // Assert
                await AssertSuccessResponseAsync(checkResponse);

                var authResult = await HttpClientHelper.DeserializeResponseAsync<dynamic>(checkResponse);
                Assert.NotNull(authResult);
            });
    }

    [Fact]
    public async Task CheckAuth_WithoutCookie_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(CheckAuth_WithoutCookie_ShouldReturnUnauthorized),
            async () =>
            {
                // Act
                var response = await GetAsync("check");

                // Assert
                await AssertUnauthorizedAsync(response);
            });
    }

    [Fact]
    public async Task CheckAuth_WithExpiredToken_ShouldReturnUnauthorized()
    {
        await RunTestWithLoggingAsync(nameof(CheckAuth_WithExpiredToken_ShouldReturnUnauthorized),
            async () =>
            {
                // Arrange
                var expiredToken = AuthenticationHelper.GenerateExpiredToken();
                var clientWithExpiredToken = CreateClientWithCookie("auth_token", expiredToken);

                // Act
                var response = await HttpClientHelper.GetAsync("/api/auth/check", clientWithExpiredToken);

                // Assert
                await AssertUnauthorizedAsync(response);
            });
    }

    #endregion

    #region 로그아웃 기능 테스트

    [Fact]
    public async Task Logout_ShouldReturnSuccessAndClearCookie()
    {
        await RunTestWithLoggingAsync(nameof(Logout_ShouldReturnSuccessAndClearCookie),
            async () =>
            {
                // Act
                var response = await PostAsync<object>(null, "logout");

                // Assert
                await AssertSuccessResponseAsync(response);

                // 쿠키 삭제 확인
                var cookies = response.Headers.GetValues("Set-Cookie").ToList();
                var authTokenCookie = cookies.FirstOrDefault(c => c.Contains("auth_token"));

                if (authTokenCookie != null)
                {
                    Assert.True(
                        authTokenCookie.Contains("expires=") ||
                        authTokenCookie.Contains("Max-Age=0"),
                        "쿠키가 삭제되지 않았습니다."
                    );
                }
            });
    }

    #endregion
    
    #region 응답 형식 테스트

    [Fact]
    public async Task GoogleLogin_ShouldReturnValidJsonFormat()
    {
        await RunTestWithLoggingAsync(nameof(GoogleLogin_ShouldReturnValidJsonFormat),
            async () =>
            {
                // Arrange
                var loginRequest = CreateValidGoogleLoginRequest();

                // Act
                var response = await PostAsync(loginRequest, "google");

                // Assert
                await AssertSuccessResponseAsync(response);

                // Content-Type 헤더 확인
                Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

                // JSON 역직렬화 가능한지 확인
                var loginResponse = await HttpClientHelper.DeserializeResponseAsync<GoogleLoginResponse>(response);
                Assert.NotNull(loginResponse);
                Assert.NotNull(loginResponse.User);

                Logger.LogInformation("JSON 형식 검증 테스트 성공");
            });
    }

    #endregion
    
    #region 통합 시나리오 테스트

    [Fact]
    public async Task CompleteAuthFlow_LoginCheckLogout_ShouldWorkEndToEnd()
    {
        await RunTestWithLoggingAsync(nameof(CompleteAuthFlow_LoginCheckLogout_ShouldWorkEndToEnd),
            async () =>
            {
                // 1. 로그인
                var loginRequest = CreateValidGoogleLoginRequest();
                var loginResponse = await PostAsync(loginRequest, "google");
                await AssertSuccessResponseAsync(loginResponse);

                var authCookie = ExtractCookieFromResponse(loginResponse, "auth_token");
                Assert.NotNull(authCookie);

                // 2. 인증 확인
                var clientWithCookie = CreateClientWithCookie("auth_token", authCookie);
                var checkResponse = await HttpClientHelper.GetAsync("/api/auth/check", clientWithCookie);
                await AssertSuccessResponseAsync(checkResponse);

                // 3. 로그아웃
                var logoutResponse = await HttpClientHelper.PostJsonAsync("/api/auth/logout", new { }, clientWithCookie);
                await AssertSuccessResponseAsync(logoutResponse);

                Logger.LogInformation("전체 인증 플로우 테스트 성공");
            });
    }

    #endregion
}