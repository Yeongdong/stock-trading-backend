namespace StockTrading.Domain.Settings;

public class KrxApiSettings
{
    public const string SectionName = "KrxApi";
    
    public string BaseUrl { get; init; } = "http://data.krx.co.kr";
    public string StockListEndpoint { get; init; } = "/comm/bldAttendant/getJsonData.cmd";
    public string BuildId { get; init; } = "dbms/MDC/STAT/standard/MDCSTAT01501";
    public int TimeoutSeconds { get; init; } = 30;
}