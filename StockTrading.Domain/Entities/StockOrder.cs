using StockTrading.Domain.Enums;

namespace StockTrading.Domain.Entities;

public class StockOrder
{
    public int Id { get; init; }
    public string StockCode { get; private set; }
    public string TradeType { get; private set; }
    public string OrderType { get; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public Market Market { get; private set; }
    public Currency Currency { get; private set; }
    public int UserId { get; private set; }
    public User? User { get; }
    public DateTime CreatedAt { get; private set; }

    public StockOrder()
    {
    }

    public StockOrder(string stockCode, string tradeType, string orderType, int quantity, decimal price,
        Market market, Currency currency, int userId)
    {
        ValidateStockCode(stockCode, market);
        ValidateTradeType(tradeType, market);
        ValidateOrderType(orderType);
        ValidateQuantity(quantity);
        ValidatePrice(price);
        ValidateUserId(userId);

        StockCode = stockCode;
        TradeType = tradeType;
        OrderType = orderType;
        Quantity = quantity;
        Price = price;
        Market = market;
        Currency = currency;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    private void ValidateStockCode(string stockCode, Market market)
    {
        if (string.IsNullOrWhiteSpace(stockCode))
            throw new ArgumentException("주식 코드는 필수입니다.", nameof(stockCode));

        if (IsKoreanMarket(market))
        {
            if (stockCode.Length != 6 || !stockCode.All(char.IsDigit))
                throw new ArgumentException("국내 주식 코드는 6자리 숫자여야 합니다.", nameof(stockCode));
        }
        else
        {
            if (stockCode.Length > 10 || !stockCode.All(c => char.IsLetterOrDigit(c)))
                throw new ArgumentException("해외 주식 코드는 10자리 이하 영숫자여야 합니다.", nameof(stockCode));
        }
    }

    private void ValidateTradeType(string tradeType, Market market)
    {
        if (string.IsNullOrWhiteSpace(tradeType))
            throw new ArgumentException("거래 유형은 필수입니다.", nameof(tradeType));

        if (IsKoreanMarket(market))
        {
            if (tradeType != "VTTC0802U" && tradeType != "VTTC0801U")
                throw new ArgumentException("국내 거래 유형은 'VTTC0802U' 또는 'VTTC0801U'이어야 합니다.", nameof(tradeType));
        }
        else
        {
            if (tradeType != "VTTT1002U" && tradeType != "VTTT1001U")
                throw new ArgumentException("해외 거래 유형은 'VTTT1002U' 또는 'VTTT1001U'이어야 합니다.", nameof(tradeType));
        }
    }

    private void ValidateOrderType(string orderType)
    {
        if (string.IsNullOrWhiteSpace(orderType))
            throw new ArgumentException("주문 유형은 필수입니다.", nameof(orderType));
    }

    private void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("수량은 0보다 커야 합니다.", nameof(quantity));
    }

    private void ValidatePrice(decimal price)
    {
        if (OrderType != "Market" && price <= 0)
            throw new ArgumentException("가격은 0보다 커야 합니다.", nameof(price));
    }

    private void ValidateUserId(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("사용자 ID는 0보다 커야 합니다.", nameof(userId));
    }

    private static bool IsKoreanMarket(Market market) =>
        market is Market.Kospi or Market.Kosdaq or Market.Konex;
}