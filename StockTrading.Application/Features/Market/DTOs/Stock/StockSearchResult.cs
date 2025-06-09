namespace StockTrading.Application.Features.Market.DTOs.Stock;

public class StockSearchResult
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? EnglishName { get; init; }
    public string Sector { get; init; } = string.Empty;
    public string Market { get; init; } = string.Empty;
}