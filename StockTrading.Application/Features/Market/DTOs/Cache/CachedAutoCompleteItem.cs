namespace StockTrading.Application.Features.Market.DTOs.Cache;

public class CachedAutoCompleteItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public int Priority { get; set; }
    public string? MatchedText { get; set; }
}

public class CachedAutoCompleteResponse
{
    public List<CachedAutoCompleteItem> Items { get; init; } = [];
    public string Prefix { get; init; } = string.Empty;
    public DateTime CachedAt { get; init; }
    public int MaxResults { get; init; } = 10;
}