namespace StockTrading.Application.Features.Market.DTOs.Stock;

/// <summary>
/// 해외주식 정보 DTO
/// </summary>
public class ForeignStockInfo
{
    /// <summary>
    /// 종목코드    
    /// </summary>
    public string Symbol { get; set; } = string.Empty;

    /// <summary>
    /// 표시용 종목코드 (실시간조회심볼)
    /// </summary>
    public string DisplaySymbol { get; set; } = string.Empty;

    /// <summary>
    /// 종목명 (한글)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 영문 종목명
    /// </summary>
    public string EnglishName { get; set; } = string.Empty;

    /// <summary>
    /// 종목 타입 (항상 "Common Stock"으로 고정)
    /// </summary>
    public string Type { get; set; } = "Common Stock";

    /// <summary>
    /// 거래소 코드
    /// </summary>
    public string Exchange { get; set; } = string.Empty;

    /// <summary>
    /// 통화 코드
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// 국가명 (거래소에 따라 매핑)
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// 현재가
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// 등락율 (%)
    /// </summary>
    public decimal ChangeRate { get; set; }

    /// <summary>
    /// 대비 (전일 대비 변동값)
    /// </summary>
    public decimal ChangeAmount { get; set; }

    /// <summary>
    /// 등락 기호 (1: 상한, 2: 상승, 3: 보합, 4: 하한, 5: 하락)
    /// </summary>
    public string ChangeSign { get; set; } = string.Empty;

    /// <summary>
    /// 거래량
    /// </summary>
    public long Volume { get; set; }

    /// <summary>
    /// 시가총액 (천 단위)
    /// </summary>
    public long MarketCap { get; set; }

    /// <summary>
    /// PER
    /// </summary>
    public decimal? PER { get; set; }

    /// <summary>
    /// EPS
    /// </summary>
    public decimal? EPS { get; set; }

    /// <summary>
    /// 매매가능 여부
    /// </summary>
    public bool IsTradable { get; set; }

    /// <summary>
    /// 순위 (거래량 기준)
    /// </summary>
    public int Rank { get; set; }
}