namespace StockTrading.Domain.Settings.ExternalServices;

public class FinnhubSettings
{
    public const string SectionName = "Finnhub";
    
    public string BaseUrl { get; set; } = "https://finnhub.io/api/v1";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public FinnhubRetrySettings RetrySettings { get; set; } = new();
    
    public FinnhubEndpoints Endpoints { get; set; } = new();
}

public class FinnhubEndpoints  
{
    public string SymbolSearchPath { get; set; } = "/search";
    public string CompanyProfilePath { get; set; } = "/stock/profile2";
    public string QuotePath { get; set; } = "/quote";
}

public class FinnhubRetrySettings
{
    public int MaxRetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}