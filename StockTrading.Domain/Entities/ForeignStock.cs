namespace StockTrading.Domain.Entities;

public class ForeignStock
{
    public int Id { get; private set; } 
    public string Symbol { get; private set; } = null!;
    public string DisplaySymbol { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public string Exchange { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string Mic { get; private set; } = null!; // Market Identifier Code
    public DateTime LastUpdated { get; private set; }

    private ForeignStock() { }

    public ForeignStock(string symbol, string displaySymbol, string description, 
        string type, string currency, string exchange, string country, string mic)
    {
        ValidateSymbol(symbol);
        ValidateDescription(description);
        ValidateExchange(exchange);

        Symbol = symbol;
        DisplaySymbol = displaySymbol;
        Description = description;
        Type = type;
        Currency = currency;
        Exchange = exchange;
        Country = country;
        Mic = mic;
        LastUpdated = DateTime.UtcNow;
    }

    private static void ValidateSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("심볼은 필수입니다.", nameof(symbol));
        
        if (symbol.Length > 20)
            throw new ArgumentException("심볼은 20자 이하여야 합니다.", nameof(symbol));
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("종목명은 필수입니다.", nameof(description));
    }

    private static void ValidateExchange(string exchange)
    {
        if (string.IsNullOrWhiteSpace(exchange))
            throw new ArgumentException("거래소 정보는 필수입니다.", nameof(exchange));
    }
}