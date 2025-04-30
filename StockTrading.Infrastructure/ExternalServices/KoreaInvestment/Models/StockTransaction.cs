namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

public record StockTransaction
{
    public string Symbol { get; init; }          // 종목코드
    public decimal Price { get; init; }          // 체결가격
    public int Volume { get; init; }             // 체결수량
    public DateTime TransactionTime { get; init; }// 체결시간
    public decimal PriceChange { get; init; }    // 전일대비
    public string ChangeType { get; init; }      // 등락구분 (상승/하락)
}