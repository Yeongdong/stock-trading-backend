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
    public bool IsScheduledOrder { get; private set; }
    public DateTime? ScheduledExecutionTime { get; private set; }
    public string? ReservedOrderNumber { get; private set; }

    public StockOrder()
    {
    }

    // 즉시주문 생성자
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
        IsScheduledOrder = false;
    }

    // 예약주문 생성자
    public StockOrder(string stockCode, string tradeType, string orderType, int quantity, decimal price,
        Market market, Currency currency, int userId, DateTime scheduledExecutionTime)
        : this(stockCode, tradeType, orderType, quantity, price, market, currency, userId)
    {
        IsScheduledOrder = true;
        ScheduledExecutionTime = scheduledExecutionTime;
    }

    public void SetReservedOrderNumber(string reservedOrderNumber)
    {
        ReservedOrderNumber = reservedOrderNumber;
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
            // 즉시주문과 예약주문 TR_ID 모두 허용
            var allowedTradeTypes = new[] { "VTTT1002U", "VTTT1001U", "VTTT3014U", "VTTT3016U", "VTTS3013U" };
            if (!allowedTradeTypes.Contains(tradeType))
                throw new ArgumentException($"해외 거래 유형은 {string.Join(", ", allowedTradeTypes)} 중 하나여야 합니다.", nameof(tradeType));
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
        if (price <= 0)
            throw new ArgumentException("가격은 0보다 커야 합니다.", nameof(price));
    }

    private void ValidateUserId(int userId)
    {
        if (userId <= 0)
            throw new ArgumentException("사용자 ID는 유효하지 않습니다.", nameof(userId));
    }

    private static bool IsKoreanMarket(Market market)
    {
        return market == Market.Kospi || market == Market.Kosdaq;
    }
}