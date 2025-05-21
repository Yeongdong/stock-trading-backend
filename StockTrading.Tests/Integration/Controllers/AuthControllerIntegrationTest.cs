using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using stock_trading_backend.DTOs;
using stock_trading_backend.Validator.Interfaces;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Controllers;

public class AuthControllerIntegrationTest : IClassFixture<StockTradingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IGoogleAuthValidator> _mockGoogleValidator;
    private readonly Mock<IUserService> _mockUserService;

    public AuthControllerIntegrationTest(StockTradingWebApplicationFactory factory)
    {
        _mockGoogleValidator = new Mock<IGoogleAuthValidator>();
        _mockJwtService = new Mock<IJwtService>();
        _mockUserService = new Mock<IUserService>();

        var testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
        };

        // Google 인증 성공 / 실패 설정
        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("valid_credential", It.IsAny<string>()))
            .ReturnsAsync(new GoogleJsonWebSignature.Payload
            {
                Email = testUser.Email,
                Name = testUser.Name,
                Subject = "user123"
            });

        _mockGoogleValidator
            .Setup(v => v.ValidateAsync("invalid_credential", It.IsAny<string>()))
            .ThrowsAsync(new Exception("Invalid credential"));

        // 사용자 생성/조회
        _mockUserService.Setup(s => s.GetOrCreateGoogleUserAsync(It.IsAny<GoogleJsonWebSignature.Payload>()))
            .ReturnsAsync(testUser);
        _mockUserService.Setup(s => s.GetUserByEmailAsync(testUser.Email))
            .ReturnsAsync(testUser);

        // JWT 생성 및 검증
        var fakeJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
                      "eyJzdWIiOiIxMjM0NTY3ODkwIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIn0." +
                      "SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        _mockJwtService.Setup(s => s.GenerateToken(It.IsAny<UserDto>()))
            .Returns(fakeJwt);

        _mockJwtService.Setup(s => s.ValidateToken(fakeJwt))
            .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, "test@example.com")
            }, "mock")));

        factory.ConfigureServices(services =>
        {
            services.AddSingleton(_mockGoogleValidator.Object);
            services.AddSingleton(_mockUserService.Object);
            services.AddSingleton(_mockJwtService.Object);
        });

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task GoogleLogin_ValidCredential_ReturnsUser_And_SetsCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest
        {
            Credential = "valid_credential"
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("test@example.com", json.GetProperty("user").GetProperty("email").GetString());

        // JWT 쿠키가 설정되었는지 확인
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), h => h.Contains("auth_token="));
    }

    [Fact]
    public async Task GoogleLogin_InvalidCredential_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest
        {
            Credential = "invalid_credential"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CheckAuth_WithValidCookie_ReturnsAuthenticatedUser()
    {
        // 먼저 로그인해서 쿠키 획득
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest
        {
            Credential = "valid_credential"
        });

        loginResponse.EnsureSuccessStatusCode();

        var checkResponse = await _client.GetAsync("/api/auth/check");
        checkResponse.EnsureSuccessStatusCode();

        var json = await checkResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal("test@example.com", json.GetProperty("user").GetProperty("email").GetString());
    }

    [Fact]
    public async Task CheckAuth_WithoutCookie_ReturnsUnauthorized()
    {
        var handler = new HttpClientHandler { UseCookies = false };
        var client = new HttpClient(handler)
        {
            BaseAddress = _client.BaseAddress
        };

        var response = await client.GetAsync("/api/auth/check");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RemovesAuthTokenCookie()
    {
        // 로그인 먼저 진행
        await _client.PostAsJsonAsync("/api/auth/google", new GoogleLoginRequest
        {
            Credential = "valid_credential"
        });

        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.EnsureSuccessStatusCode();

        var cookies = logoutResponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.StartsWith("auth_token=;"));
    }
}
