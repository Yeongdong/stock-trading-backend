namespace StockTrading.Domain.Entities;

public class StockOrder
{
    public int Id { get; set; }
    public string StockCode { get; private set; }
    public string TradeType { get; private set; }
    public string OrderType { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public StockOrder()
    {
    }

    public StockOrder(string stockCode, string tradeType, string orderType, int quantity, decimal price, User user)
    {
        ValidateStockCode(stockCode);
        ValidateTradeType(tradeType);
        ValidateOrderType(orderType);
        ValidateQuantity(quantity);
        ValidatePrice(price);
        ValidateUser(user);

        StockCode = stockCode;
        TradeType = tradeType;
        OrderType = orderType;
        Quantity = quantity;
        Price = price;
        User = user;
        UserId = user.Id;
        CreatedAt = DateTime.UtcNow;
    }

    private void ValidateStockCode(string stockCode)
    {
        if (string.IsNullOrWhiteSpace(stockCode))
        {
            throw new ArgumentException("주식 코드는 필수입니다.", nameof(stockCode));
        }
        
        if (stockCode.Length != 6 || !stockCode.All(char.IsDigit))
        {
            throw new ArgumentException("주식 코드는 6자리 숫자여야 합니다.", nameof(stockCode));
        }
    }

    private void ValidateTradeType(string tradeType)
    {
        if (string.IsNullOrWhiteSpace(tradeType))
        {
            throw new ArgumentException("거래 유형은 필수입니다.", nameof(tradeType));
        }
        
        if (tradeType != "VTTC0802U" && tradeType != "VTTC0801U")
        {
            throw new ArgumentException("거래 유형은 'VTTC0802U' 또는 'VTTC0801U'이어야 합니다.", nameof(tradeType));
        }
    }

    private void ValidateOrderType(string orderType)
    {
        if (string.IsNullOrWhiteSpace(orderType))
        {
            throw new ArgumentException("주문 유형은 필수입니다.", nameof(orderType));
        }
        
        // // 주문 유형 검증 (Limit 또는 Market만 허용)
        // if (orderType != "Limit" && orderType != "Market")
        // {
        //     throw new ArgumentException("주문 유형은 'Limit' 또는 'Market'이어야 합니다.", nameof(orderType));
        // }
    }

    private void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("수량은 0보다 커야 합니다.", nameof(quantity));
        }
    }

    private void ValidatePrice(decimal price)
    {
        // Market 주문이 아닌 경우 가격 검증
        if (OrderType != "Market" && price <= 0)
        {
            throw new ArgumentException("가격은 0보다 커야 합니다.", nameof(price));
        }
    }

    private void ValidateUser(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "사용자 정보는 필수입니다.");
        }
    }
}