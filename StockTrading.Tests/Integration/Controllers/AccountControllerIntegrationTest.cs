using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Tests.Integration.Controllers;

public class AccountControllerIntegrationTest : IClassFixture<StockTradingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly StockTradingWebApplicationFactory _factory;

    public AccountControllerIntegrationTest(StockTradingWebApplicationFactory factory)
    {
        _factory = factory;
        var kisServiceMock = new Mock<IKisService>();
        var googleAuthProviderMock = new Mock<IGoogleAuthProvider>();

        kisServiceMock
            .Setup(service => service.UpdateUserKisInfoAndTokensAsync(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TokenResponse
            {
                AccessToken = "test_access_token",
                TokenType = "Bearer",
                ExpiresIn = 3600
            });

        googleAuthProviderMock
            .Setup(provider => provider.GetUserInfoAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new GoogleUserInfo
            {
                Email = "test@example.com",
                Name = "Test User"
            });

        _factory.ConfigureServices(services =>
        {
            services.AddScoped<IKisService>(sp => kisServiceMock.Object);
            services.AddScoped<IGoogleAuthProvider>(sp => googleAuthProviderMock.Object);
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task UpdateUserInfo_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new UserInfoRequest
        {
            AppKey = "test_app_key",
            AppSecret = "test_app_secret",
            AccountNumber = "test_account"
        };

        var response = await _client.PostAsJsonAsync("/api/account/userInfo", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserInfo_WithValidAuth_ReturnsOk()
    {
        var token = GetJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UserInfoRequest
        {
            AppKey = "test_app_key",
            AppSecret = "test_app_secret",
            AccountNumber = "test_account"
        };

        var response = await _client.PostAsJsonAsync("/api/account/userInfo", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(tokenResponse);
        Assert.NotNull(tokenResponse.AccessToken);
    }

    private string GetJwtToken()
    {
        using var scope = _factory.Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();

        var testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        return jwtService.GenerateToken(testUser);
    }
}