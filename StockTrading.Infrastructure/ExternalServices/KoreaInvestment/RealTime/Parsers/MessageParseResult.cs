namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.Parsers;

public record MessageParseResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public string? TrId { get; private init; }
    public string? Data { get; private init; }
    public int DataCount { get; private init; }

    public static MessageParseResult Success(string? trId, string? data, int dataCount = 1) =>
        new() { IsSuccess = true, TrId = trId, Data = data, DataCount = dataCount };

    public static MessageParseResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}