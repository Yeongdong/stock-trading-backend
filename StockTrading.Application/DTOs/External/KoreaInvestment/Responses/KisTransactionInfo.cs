using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public record KisTransactionInfo
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; init; }          // 종목코드
    
    [JsonPropertyName("price")]
    public decimal Price { get; init; }          // 체결가격
    
    [JsonPropertyName("volume")]
    public int Volume { get; init; }             // 체결수량
    
    [JsonPropertyName("transactionTime")]
    public DateTime TransactionTime { get; init; }// 체결시간
    
    [JsonPropertyName("priceChange")]
    public decimal PriceChange { get; init; }    // 전일대비
    
    [JsonPropertyName("changeType")]
    public string ChangeType { get; init; }      // 등락구분 (상승/하락)
    
    [JsonPropertyName("changeRate")]
    public decimal ChangeRate { get; init; }     // 등락률
    
    [JsonPropertyName("totalVolume")]
    public long TotalVolume { get; init; }       // 누적거래량
    
    [JsonPropertyName("openPrice")]
    public decimal OpenPrice { get; init; }      // 시가
    
    [JsonPropertyName("highPrice")]
    public decimal HighPrice { get; init; }      // 고가
    
    [JsonPropertyName("lowPrice")]
    public decimal LowPrice { get; init; }       // 저가
}