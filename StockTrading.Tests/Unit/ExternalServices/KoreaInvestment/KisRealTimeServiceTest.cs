using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.Tests.Unit.ExternalServices.KoreaInvestment;

[TestSubject(typeof(KisRealTimeService))]
public class KisRealTimeServiceTest
{
    private readonly Mock<IKisWebSocketClient> _mockWebSocketClient;
    private readonly Mock<IKisRealTimeDataProcessor> _mockDataProcessor;
    private readonly Mock<IKisSubscriptionManager> _mockSubscriptionManager;
    private readonly Mock<IRealTimeDataBroadcaster> _mockBroadcaster;
    private readonly Mock<ILogger<KisRealTimeService>> _mockLogger;
    private readonly KisRealTimeService _service;

    public KisRealTimeServiceTest()
    {
        _mockWebSocketClient = new Mock<IKisWebSocketClient>();
        _mockDataProcessor = new Mock<IKisRealTimeDataProcessor>();
        _mockSubscriptionManager = new Mock<IKisSubscriptionManager>();
        _mockBroadcaster = new Mock<IRealTimeDataBroadcaster>();
        _mockLogger = new Mock<ILogger<KisRealTimeService>>();

        _service = new KisRealTimeService(
            _mockWebSocketClient.Object,
            _mockDataProcessor.Object,
            _mockSubscriptionManager.Object,
            _mockBroadcaster.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task StartAsync_CallsWebSocketClientConnect()
    {
        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync();

        _mockWebSocketClient.Verify(c => c.ConnectAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithUser_CallsAuthenticateAsync()
    {
        var user = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            WebSocketToken = "test_token"
        };

        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync(user);

        _mockWebSocketClient.Verify(c => c.ConnectAsync(It.IsAny<string>()), Times.Once);
        _mockWebSocketClient.Verify(c => c.AuthenticateAsync(user.WebSocketToken), Times.Once);
    }
    
    [Fact]
    public async Task SubscribeSymbolAsync_CallsSubscriptionManager()
    {
        string symbol = "005930";
            
        _mockSubscriptionManager
            .Setup(m => m.SubscribeSymbolAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.SubscribeSymbolAsync(symbol);

        _mockSubscriptionManager.Verify(m => m.SubscribeSymbolAsync(symbol), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeSymbolAsync_CallsSubscriptionManager()
    {
        string symbol = "005930";
            
        _mockSubscriptionManager
            .Setup(m => m.UnsubscribeSymbolAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.UnsubscribeSymbolAsync(symbol);

        _mockSubscriptionManager.Verify(m => m.UnsubscribeSymbolAsync(symbol), Times.Once);
    }
    
    [Fact]
    public void GetSubscribedSymbols_CallsSubscriptionManager()
    {
        var expectedSymbols = new List<string> { "005930", "000660" };
            
        _mockSubscriptionManager
            .Setup(m => m.GetSubscribedSymbols())
            .Returns(expectedSymbols);

        var result = _service.GetSubscribedSymbols();

        _mockSubscriptionManager.Verify(m => m.GetSubscribedSymbols(), Times.Once);
        Assert.Equal(expectedSymbols, result);
    }

    [Fact]
    public async Task StopAsync_CallsUnsubscribeAllAndDisconnect()
    {
        _mockSubscriptionManager
            .Setup(m => m.UnsubscribeAllAsync())
            .Returns(Task.CompletedTask);
                
        _mockWebSocketClient
            .Setup(c => c.DisconnectAsync())
            .Returns(Task.CompletedTask);

        await _service.StopAsync();

        _mockSubscriptionManager.Verify(m => m.UnsubscribeAllAsync(), Times.Once);
        _mockWebSocketClient.Verify(c => c.DisconnectAsync(), Times.Once);
    }
}