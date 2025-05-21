using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using stock_trading_backend.Controllers;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(RealTimeController))]
public class RealTimeControllerTest
{
    private readonly Mock<IKisRealTimeService> _mockRealTimeService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<RealTimeController>> _mockLogger;
    private readonly RealTimeController _controller;
    private readonly UserDto _testUser;

    public RealTimeControllerTest()
    {
        _mockRealTimeService = new Mock<IKisRealTimeService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<RealTimeController>>();
        _controller = new RealTimeController(
            _mockRealTimeService.Object,
            _mockUserService.Object,
            _mockLogger.Object
        );

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = "test_token"
        };

        // 사용자 인증 컨텍스트 설정
        var identity = new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Email, _testUser.Email),
        });
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };

        _mockUserService
            .Setup(s => s.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync(_testUser);
    }
    
    [Fact]
    public async Task StartRealTimeService_ReturnsOk_WhenSuccessful()
    {
        _mockRealTimeService
            .Setup(s => s.StartAsync(It.IsAny<UserDto>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.StartRealTimeService();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockRealTimeService.Verify(s => s.StartAsync(_testUser), Times.Once);
    }

    [Fact]
    public async Task StartRealTimeService_ReturnsBadRequest_WhenUserHasNoToken()
    {
        var userWithoutToken = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = null
        };

        _mockUserService
            .Setup(s => s.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync(userWithoutToken);

        var result = await _controller.StartRealTimeService();

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("WebSocket 토큰 없음", badRequestResult.Value.ToString());
        _mockRealTimeService.Verify(s => s.StartAsync(It.IsAny<UserDto>()), Times.Never);
    }
    
    [Fact]
    public async Task StopRealTimeService_ReturnsOk_WhenSuccessful()
    {
        _mockRealTimeService
            .Setup(s => s.StopAsync())
            .Returns(Task.CompletedTask);

        var result = await _controller.StopRealTimeService();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockRealTimeService.Verify(s => s.StopAsync(), Times.Once);
    }
    
    [Fact]
    public async Task SubscribeSymbol_ReturnsOk_WhenSuccessful()
    {
        string symbol = "005930";
            
        _mockRealTimeService
            .Setup(s => s.SubscribeSymbolAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.SubscribeSymbol(symbol);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockRealTimeService.Verify(s => s.SubscribeSymbolAsync(symbol), Times.Once);
    }
    
    [Fact]
    public async Task SubscribeSymbol_ReturnsBadRequest_WhenSymbolIsInvalid()
    {
        string invalidSymbol = "12345"; // 5자리 (6자리여야 함)

        var result = await _controller.SubscribeSymbol(invalidSymbol);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("유효하지 않은 종목 코드", badRequestResult.Value.ToString());
        _mockRealTimeService.Verify(s => s.SubscribeSymbolAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task UnsubscribeSymbol_ReturnsOk_WhenSuccessful()
    {
        string symbol = "005930";
            
        _mockRealTimeService
            .Setup(s => s.UnsubscribeSymbolAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.UnsubscribeSymbol(symbol);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockRealTimeService.Verify(s => s.UnsubscribeSymbolAsync(symbol), Times.Once);
    }
    
    [Fact]
    public void GetSubscriptions_ReturnsOk_WithSymbolsList()
    {
        var subscribedSymbols = new List<string> { "005930", "000660" };
            
        _mockRealTimeService
            .Setup(s => s.GetSubscribedSymbols())
            .Returns(subscribedSymbols);

        var result = _controller.GetSubscriptions();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
            
        // 원본 컨트롤러 메서드 반환값(익명 타입) 확인
        // => return Ok(new { symbols = _realTimeService.GetSubscribedSymbols() });
        // 리플렉션을 사용하여 속성에 접근
        var resultObj = okResult.Value;
        var symbolsProp = resultObj.GetType().GetProperty("symbols");
        Assert.NotNull(symbolsProp);
            
        var symbolsValue = symbolsProp.GetValue(resultObj);
        Assert.NotNull(symbolsValue);
            
        // 직접 컬렉션으로 변환하지 않고 Count 메서드 사용
        var count = (symbolsValue as IEnumerable<string>)?.Count() ?? 0;
        Assert.Equal(subscribedSymbols.Count, count);
            
        _mockRealTimeService.Verify(s => s.GetSubscribedSymbols(), Times.Once);
    }
}