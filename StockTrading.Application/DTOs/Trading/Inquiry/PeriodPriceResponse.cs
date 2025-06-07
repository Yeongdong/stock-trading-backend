namespace StockTrading.Application.DTOs.Trading.Inquiry;

public class PeriodPriceResponse
{
    public string StockCode { get; set; } = string.Empty;
    public string StockName { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange { get; set; }
    public decimal ChangeRate { get; set; }
    public string ChangeSign { get; set; } = string.Empty;
    public long TotalVolume { get; set; }
    public long TotalTradingValue { get; set; }
    public List<PeriodPriceData> PriceData { get; set; } = [];
}

public class PeriodPriceData
{
    public string Date { get; set; } = string.Empty;
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public long Volume { get; set; }
    public long TradingValue { get; set; }
    public decimal PriceChange { get; set; }
    public string ChangeSign { get; set; } = string.Empty;
    public string FlagCode { get; set; } = string.Empty;
    public decimal SplitRate { get; set; }
}