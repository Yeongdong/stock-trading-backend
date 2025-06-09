using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.Tests.Unit.ExternalServices.KoreaInvestment;

[TestSubject(typeof(SubscriptionManager))]
public class SubscriptionManagerTest
{
    private readonly Mock<IWebSocketClient> _mockWebSocketClient;
    private readonly Mock<ILogger<SubscriptionManager>> _mockLogger;
    private readonly SubscriptionManager _manager;

    public SubscriptionManagerTest()
    {
        _mockWebSocketClient = new Mock<IWebSocketClient>();
        _mockLogger = new Mock<ILogger<SubscriptionManager>>();
        _manager = new SubscriptionManager(_mockWebSocketClient.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SubscribeSymbolAsync_SendsCorrectMessage()
    {
        string symbol = "005930";
        string sentMessage = null;

        _mockWebSocketClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(msg => sentMessage = msg)
            .Returns(Task.CompletedTask);

        await _manager.SubscribeSymbolAsync(symbol);

        _mockWebSocketClient.Verify(c => c.SendMessageAsync(It.IsAny<string>()), Times.Once);
        Assert.Contains("tr_type", sentMessage);
        Assert.Contains("1", sentMessage); // 1: 등록
        Assert.Contains("tr_id", sentMessage);
        Assert.Contains("H0STASP0", sentMessage);
        Assert.Contains("tr_key", sentMessage);
        Assert.Contains(symbol, sentMessage);
    }

    [Fact]
    public async Task UnsubscribeSymbolAsync_SendsCorrectMessage()
    {
        string symbol = "005930";
        string sentMessage = null;

        // 먼저 구독해야 구독 해제 가능
        _mockWebSocketClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>()))
            .Callback<string>(msg => sentMessage = msg)
            .Returns(Task.CompletedTask);
        await _manager.SubscribeSymbolAsync(symbol);
        sentMessage = null; // 메시지 초기화

        await _manager.UnsubscribeSymbolAsync(symbol);

        _mockWebSocketClient.Verify(c => c.SendMessageAsync(It.IsAny<string>()), Times.Exactly(2));
        Assert.Contains("tr_type", sentMessage);
        Assert.Contains("2", sentMessage); // 2: 해제
        Assert.Contains("tr_id", sentMessage);
        Assert.Contains("H0STASP0", sentMessage);
        Assert.Contains("tr_key", sentMessage);
        Assert.Contains(symbol, sentMessage);
    }
    
    [Fact]
    public async Task GetSubscribedSymbols_ReturnsCorrectList()
    {
        _mockWebSocketClient
            .Setup(c => c.SendMessageAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _manager.SubscribeSymbolAsync("005930");
        await _manager.SubscribeSymbolAsync("000660");
        var symbols = _manager.GetSubscribedSymbols();

        Assert.Equal(2, symbols.Count);
        Assert.Contains("005930", symbols);
        Assert.Contains("000660", symbols);
    }
}