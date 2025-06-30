namespace StockTrading.Application.Features.Market.DTOs.Stock;

/// <summary>
/// KIS API 해외주식 조건검색 결과 DTO
/// </summary>
public class ForeignStockSearchResult
{
    /// <summary>
    /// 검색된 주식 목록
    /// </summary>
    public List<ForeignStockInfo> Stocks { get; set; } = [];

    /// <summary>
    /// 총 검색 결과 수
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// 검색된 시장
    /// </summary>
    public string Market { get; set; } = string.Empty;

    /// <summary>
    /// 시세 상태 정보
    /// </summary>
    public string Status { get; set; } = string.Empty;
}