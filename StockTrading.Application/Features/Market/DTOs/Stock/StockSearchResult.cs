namespace StockTrading.Application.Features.Market.DTOs.Stock;

public class StockSearchResult
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? EnglishName { get; init; }
    public string Sector { get; init; } = string.Empty;
    public string Market { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string? StockType { get; init; }
    public DateTime? ListedDate { get; init; }
    public DateTime LastUpdated { get; init; }
}