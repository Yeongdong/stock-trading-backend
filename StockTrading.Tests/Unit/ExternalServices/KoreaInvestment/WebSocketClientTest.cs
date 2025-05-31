using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.Tests.Unit.ExternalServices.KoreaInvestment;

[TestSubject(typeof(WebSocketClient))]
public class WebSocketClientTest
{
    private readonly Mock<ILogger<WebSocketClient>> _mockLogger;
    private readonly WebSocketClient _client;

    public WebSocketClientTest()
    {
        _mockLogger = new Mock<ILogger<WebSocketClient>>();
        _client = new WebSocketClient(_mockLogger.Object);
    }

    [Fact]
    public void MessageReceived_EventRaised_WhenEventHandlerAdded()
    {
        bool eventRaised = false;
        string testMessage = "test message";

        _client.MessageReceived += (sender, message) =>
        {
            eventRaised = true;
            Assert.Equal(testMessage, message);
        };

        var methodInfo = typeof(WebSocketClient).GetMethod("OnMessageReceived",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        methodInfo.Invoke(_client, new object[] { testMessage });

        Assert.True(eventRaised);
    }

    [Fact]
    public void METHOD()
    {
        
    }
}