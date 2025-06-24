using FluentAssertions;
using JetBrains.Annotations;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;

namespace StockTrading.Tests.Unit.Entities;

[TestSubject(typeof(StockOrder))]
public class StockOrderTest
{
    [Fact]
    public void StockOrder_Constructor_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        string stockCode = "005930";
        string tradeType = "VTTC0802U";
        string orderType = "00";
        int quantity = 10;
        decimal price = 70_000;
        int userId = 1;

        // Act
        var stockOrder = new StockOrder(stockCode, tradeType, orderType, quantity, price, Market.Kospi, Currency.Krw, userId);

        // Assert
        stockOrder.StockCode.Should().Be(stockCode);
        stockOrder.TradeType.Should().Be(tradeType);
        stockOrder.OrderType.Should().Be(orderType);
        stockOrder.Quantity.Should().Be(quantity);
        stockOrder.Price.Should().Be(price);
        stockOrder.UserId.Should().Be(userId);
    }

    [Theory]
    [InlineData("", "Buy", "Limit", 10, 1000)]
    [InlineData("005930", "", "Limit", 10, 1000)]
    [InlineData("005930", "Buy", "", 10, 1000)]
    [InlineData("005930", "Buy", "Limit", 0, 1000)]
    [InlineData("005930", "Buy", "Limit", 10, 0)]
    public void StockOrder_WithInvalidParameters_ShouldThrowException(string stockCode, string tradeType,
        string orderType, int quantity, decimal price)
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com"
        };

        Action act = () => new StockOrder(stockCode, tradeType, orderType, quantity, price, Market.Kospi, Currency.Krw, user.Id);
        act.Should().Throw<ArgumentException>();
    }
}