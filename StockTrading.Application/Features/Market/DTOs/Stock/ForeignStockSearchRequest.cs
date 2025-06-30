using System.ComponentModel.DataAnnotations;

namespace StockTrading.Application.Features.Market.DTOs.Stock;

/// <summary>
/// KIS API 해외주식 조건검색 요청 DTO
/// </summary>
public class ForeignStockSearchRequest
{
    /// <summary>
    /// 거래소코드 (NYS: 뉴욕, NAS: 나스닥, AMS: 아멕스, HKS: 홍콩, SHS: 상해, SZS: 심천, HSX: 호치민, HNX: 하노이, TSE: 도쿄)
    /// </summary>
    [Required]
    public string Market { get; set; } = string.Empty;

    /// <summary>
    /// 검색어 (종목명 등) - 현재가 범위로 검색하기 위한 필터링용
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// 현재가 최소값 (해당 통화 단위)
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// 현재가 최대값 (해당 통화 단위)
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// 등락율 최소값 (%)
    /// </summary>
    public decimal? MinChangeRate { get; set; }

    /// <summary>
    /// 등락율 최대값 (%)
    /// </summary>
    public decimal? MaxChangeRate { get; set; }

    /// <summary>
    /// 거래량 최소값
    /// </summary>
    public long? MinVolume { get; set; }

    /// <summary>
    /// 거래량 최대값
    /// </summary>
    public long? MaxVolume { get; set; }

    /// <summary>
    /// 조회 결과 제한 수 (최대 100)
    /// </summary>
    public int Limit { get; set; } = 50;
}