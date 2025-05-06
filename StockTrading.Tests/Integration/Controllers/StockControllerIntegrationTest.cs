using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTradingBackend.DataAccess.Interfaces;

namespace StockTrading.Tests.Integration.Controllers;

public class StockControllerIntegrationTests : IClassFixture<StockTradingWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly StockTradingWebApplicationFactory _factory;
    
    public StockControllerIntegrationTests(StockTradingWebApplicationFactory factory)
    {
        _factory = factory;
        
        var kisServiceMock = new Mock<IKisService>();
        var userServiceMock = new Mock<IUserService>();
        
        var testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };
        
        userServiceMock
            .Setup(service => service.GetUserByEmailAsync("test@example.com"))
            .ReturnsAsync(testUser);
        
        kisServiceMock
            .Setup(service => service.GetStockBalanceAsync(It.IsAny<UserDto>()))
            .ReturnsAsync(new StockBalance
            {
                Positions = new List<Position>(),
                Summary = new Summary
                {
                    TotalDeposit = "1000000",
                    StockEvaluation = "500000",
                    TotalEvaluation = "1500000"
                }
            });
            
        kisServiceMock
            .Setup(service => service.PlaceOrderAsync(It.IsAny<StockOrderRequest>(), It.IsAny<UserDto>()))
            .ReturnsAsync(new StockOrderResponse
            {
                rt_cd = "0",
                msg_cd = "MSG_0001",
                msg = "정상처리 되었습니다.",
                output = new OrderOutput
                {
                    ODNO = "123456789",
                    KRX_FWDG_ORD_ORGNO = "12345",
                    ORD_TMD = "102030"
                }
            });
            
        _factory.ConfigureServices(services =>
        {
            services.AddScoped<IKisService>(sp => kisServiceMock.Object);
            services.AddScoped<IUserService>(sp => userServiceMock.Object);
        });
        
        _client = _factory.CreateClient();
        
        var token = GetJwtToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
    
    [Fact]
    public async Task GetBalance_WithValidAuth_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/stock/balance");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var balance = await response.Content.ReadFromJsonAsync<StockBalance>();
        Assert.NotNull(balance);
        Assert.NotNull(balance.Summary);
        Assert.Equal("1000000", balance.Summary.TotalDeposit);
        Assert.Equal("500000", balance.Summary.StockEvaluation);
        Assert.Equal("1500000", balance.Summary.TotalEvaluation);
    }
    
    [Fact]
    public async Task PlaceOrder_WithValidAuth_ReturnsOk()
    {
        var orderRequest = new StockOrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };
        
        var response = await _client.PostAsJsonAsync("/api/stock/order", orderRequest);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var orderResponse = await response.Content.ReadFromJsonAsync<StockOrderResponse>();
        Assert.NotNull(orderResponse);
        Assert.Equal("0", orderResponse.rt_cd);
        Assert.NotNull(orderResponse.output);
        Assert.Equal("123456789", orderResponse.output.ODNO);
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