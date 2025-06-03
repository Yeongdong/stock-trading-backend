namespace StockTrading.Domain.Settings;

public class KrxApiSettings
{
    public const string SectionName = "KrxApi";
    
    /// <summary>
    /// KRX OpenAPI 기본 URL
    /// </summary>
    public string BaseUrl { get; init; } = "https://data.krx.co.kr";
    
    /// <summary>
    /// 주식종목 정보 조회 엔드포인트
    /// </summary>
    public string StockListEndpoint { get; init; } = "/comm/bldAttendant/getJsonData.cmd";
    
    /// <summary>
    /// 주식종목 정보 Build ID (MDCSTAT01901)
    /// </summary>
    public string StockListBuildId { get; init; } = "dbms/MDC/STAT/standard/MDCSTAT01901";
    
    /// <summary>
    /// User-Agent 헤더 값
    /// </summary>
    public string UserAgent { get; init; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
    
    /// <summary>
    /// HTTP 요청 타임아웃 (초)
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
    
    /// <summary>
    /// 재시도 횟수
    /// </summary>
    public int RetryCount { get; init; } = 3;
    
    /// <summary>
    /// 재시도 간격 (밀리초)
    /// </summary>
    public int RetryDelayMs { get; init; } = 1000;
}