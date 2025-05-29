using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.DTOs.Users;
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
    public async Task StartAsync_WithUser_CallsConnectAndAuthenticate()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = "test_token"
        };

        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(user);

        // Assert
        _mockWebSocketClient.Verify(c => c.ConnectAsync("ws://ops.koreainvestment.com:31000"), Times.Once);
        _mockWebSocketClient.Verify(c => c.AuthenticateAsync(user.WebSocketToken), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyStarted_DoesNotConnectAgain()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = "test_token"
        };

        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(user);
        await _service.StartAsync(user);  // 두 번째 호출

        // Assert
        _mockWebSocketClient.Verify(c => c.ConnectAsync(It.IsAny<string>()), Times.Once);
        _mockWebSocketClient.Verify(c => c.AuthenticateAsync(It.IsAny<string>()), Times.Once);
    }
    
    [Fact]
    public async Task SubscribeSymbolAsync_WhenServiceNotStarted_ThrowsException()
    {
        // Arrange
        string symbol = "005930";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SubscribeSymbolAsync(symbol));
        
        Assert.Equal("서비스를 먼저 시작하세요", exception.Message);
    }

    [Fact]
    public async Task SubscribeSymbolAsync_WhenServiceStarted_CallsSubscriptionManager()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = "test_token"
        };
        string symbol = "005930";

        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockSubscriptionManager
            .Setup(m => m.SubscribeSymbolAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(user);
        await _service.SubscribeSymbolAsync(symbol);

        // Assert
        _mockSubscriptionManager.Verify(m => m.SubscribeSymbolAsync(symbol), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeSymbolAsync_WhenServiceStarted_CallsSubscriptionManager()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = "test_token"
        };
        string symbol = "005930";

        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockSubscriptionManager
            .Setup(m => m.UnsubscribeSymbolAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(user);
        await _service.UnsubscribeSymbolAsync(symbol);

        // Assert
        _mockSubscriptionManager.Verify(m => m.UnsubscribeSymbolAsync(symbol), Times.Once);
    }

    [Fact]
    public async Task UnsubscribeSymbolAsync_WhenServiceNotStarted_DoesNotCallSubscriptionManager()
    {
        // Arrange
        string symbol = "005930";

        // Act
        await _service.UnsubscribeSymbolAsync(symbol);

        // Assert
        _mockSubscriptionManager.Verify(m => m.UnsubscribeSymbolAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public void GetSubscribedSymbols_CallsSubscriptionManager()
    {
        // Arrange
        var expectedSymbols = new List<string> { "005930", "000660" };
            
        _mockSubscriptionManager
            .Setup(m => m.GetSubscribedSymbols())
            .Returns(expectedSymbols);

        // Act
        var result = _service.GetSubscribedSymbols();

        // Assert
        _mockSubscriptionManager.Verify(m => m.GetSubscribedSymbols(), Times.Once);
        Assert.Equal(expectedSymbols, result);
    }

    [Fact]
    public async Task StopAsync_WhenServiceNotStarted_DoesNothing()
    {
        // Act
        await _service.StopAsync();

        // Assert
        _mockSubscriptionManager.Verify(m => m.UnsubscribeAllAsync(), Times.Never);
        _mockWebSocketClient.Verify(c => c.DisconnectAsync(), Times.Never);
    }

    [Fact]
    public async Task StopAsync_WhenServiceStarted_CallsUnsubscribeAllAndDisconnect()
    {
        // Arrange
        var user = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            WebSocketToken = "test_token"
        };

        _mockWebSocketClient
            .Setup(c => c.ConnectAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockWebSocketClient
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockSubscriptionManager
            .Setup(m => m.UnsubscribeAllAsync())
            .Returns(Task.CompletedTask);
                
        _mockWebSocketClient
            .Setup(c => c.DisconnectAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(user);
        await _service.StopAsync();

        // Assert
        _mockSubscriptionManager.Verify(m => m.UnsubscribeAllAsync(), Times.Once);
        _mockWebSocketClient.Verify(c => c.DisconnectAsync(), Times.Once);
    }
}