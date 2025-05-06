using System.Net;
using System.Net.Http.Json;
using Google.Apis.Auth;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using stock_trading_backend.DTOs;
using stock_trading_backend.Interfaces;

namespace StockTrading.Tests.Integration.Controllers;

public class AuthControllerIntegrationTest : IClassFixture<StockTradingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly StockTradingWebApplicationFactory _factory;
    private readonly Mock<IGoogleAuthValidator> _mockGoogleAuthValidator;

    public AuthControllerIntegrationTest(StockTradingWebApplicationFactory factory)
    {
        _factory = factory;
        
        _mockGoogleAuthValidator = new Mock<IGoogleAuthValidator>();
        
        _mockGoogleAuthValidator
            .Setup(validator => validator.ValidateAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GoogleJsonWebSignature.Payload
            {
                Email = "test@example.com",
                Name = "Test User",
                Subject = "test_google_id"
            });
            
        _mockGoogleAuthValidator
            .Setup(validator => validator.ValidateAsync("invalid_credential", It.IsAny<string>()))
            .ThrowsAsync(new Exception("Invalid token"));
            
        _factory.ConfigureServices(services =>
        {
            services.AddScoped<IGoogleAuthValidator>(sp => _mockGoogleAuthValidator.Object);
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GoogleLogin_WithValidCredential_ReturnsOk()
    {
        var request = new GoogleLoginRequest
        {
            Credential = "valid_test_credential"
        };
        
        var response = await _client.PostAsJsonAsync("/api/auth/google", request);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loginResponse = await response.Content.ReadFromJsonAsync<GoogleLoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotNull(loginResponse.Token);
        Assert.NotNull(loginResponse.User);
        Assert.Equal("test@example.com", loginResponse.User.Email);
    }
    
    [Fact]
    public async Task GoogleLogin_WithInvalidCredential_ReturnsBadRequest()
    {
        var request = new GoogleLoginRequest
        {
            Credential = "invalid_credential"
        };
        
        var response = await _client.PostAsJsonAsync("/api/auth/google", request);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}