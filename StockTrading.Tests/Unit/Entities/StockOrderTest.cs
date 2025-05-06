using FluentAssertions;
using JetBrains.Annotations;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Unit.Entities;

[TestSubject(typeof(StockOrder))]
public class StockOrderTest
{
    [Fact]
    public void StockOrder_Constructor_ShouldInitializePropertiesCorrectly()
    {
        string stockCode = "005930";
        string tradeType = "Buy";
        string orderType = "Limit";
        int quantity = 10;
        decimal price = 70_000;
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            Name = "Test",
            GoogleId = "google_test",
            CreatedAt = DateTime.Now,
            Role = "User"
        };
        
        var stockOrder = new StockOrder(stockCode, tradeType, orderType, quantity, price, user);
        stockOrder.StockCode.Should().Be(stockCode);
        stockOrder.TradeType.Should().Be(tradeType);
        stockOrder.OrderType.Should().Be(orderType);
        stockOrder.Quantity.Should().Be(quantity);
        stockOrder.Price.Should().Be(price);
        stockOrder.User.Should().BeSameAs(user);
    }
    
    [Theory]
    [InlineData("", "Buy", "Limit", 10, 1000)]
    [InlineData("005930", "", "Limit", 10, 1000)]
    [InlineData("005930", "Buy", "", 10, 1000)]
    [InlineData("005930", "Buy", "Limit", 0, 1000)]
    [InlineData("005930", "Buy", "Limit", 10, 0)]
    public void StockOrder_WithInvalidParameters_ShouldThrowException(string stockCode, string tradeType, string orderType, int quantity, decimal price)
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com"
        };

        Action act = () => new StockOrder(stockCode, tradeType, orderType, quantity, price, user);
        act.Should().Throw<ArgumentException>();
    }
}