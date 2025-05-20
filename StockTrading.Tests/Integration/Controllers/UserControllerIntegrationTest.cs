using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using static System.Net.HttpStatusCode;

namespace StockTrading.Tests.Integration.Controllers;

public class UserControllerIntegrationTest : IClassFixture<StockTradingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly StockTradingWebApplicationFactory _factory;

    public UserControllerIntegrationTest(StockTradingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/user");
        
        Assert.Equal(Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task GetCurrentUser_WithValidAuth_ReturnsOk()
    {
        var token = GetJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.GetAsync("/api/user");
        
        Assert.Equal(OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
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