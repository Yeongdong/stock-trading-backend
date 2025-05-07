using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.Tests.ExternalServices.KoreaInvestment;

[TestSubject(typeof(KisWebSocketClient))]
public class KisWebSocketClientTest
{
    private readonly Mock<ILogger<KisWebSocketClient>> _mockLogger;
    private readonly KisWebSocketClient _client;

    public KisWebSocketClientTest()
    {
        _mockLogger = new Mock<ILogger<KisWebSocketClient>>();
        _client = new KisWebSocketClient(_mockLogger.Object);
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

        var methodInfo = typeof(KisWebSocketClient).GetMethod("OnMessageReceived",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        methodInfo.Invoke(_client, new object[] { testMessage });

        Assert.True(eventRaised);
    }

    [Fact]
    public void METHOD()
    {
        
    }
}