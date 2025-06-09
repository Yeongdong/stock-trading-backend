using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Domain.Settings;
using StockTrading.Domain.Settings.ExternalServices;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.Parsers;

public class JsonMessageParser
{
    private readonly ILogger<JsonMessageParser> _logger;
    private readonly RealTimeDataSettings _settings;

    public JsonMessageParser(ILogger<JsonMessageParser> logger, RealTimeDataSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public MessageParseResult Parse(string? messageJson)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(messageJson);
            var root = jsonDoc.RootElement;

            if (!TryGetTrId(root, out var trId))
            {
                return MessageParseResult.Failure("TR ID를 찾을 수 없습니다.");
            }

            if (!TryGetSubscriptionResponse(root, out var subscriptionMessage))
                return MessageParseResult.Success(trId, messageJson);
            
            _logger.LogInformation("구독 응답: {Message}", subscriptionMessage);
            return MessageParseResult.Success(trId, subscriptionMessage);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 파싱 실패: {Message}", messageJson);
            return MessageParseResult.Failure($"JSON 파싱 실패: {ex.Message}");
        }
    }

    private static bool TryGetTrId(JsonElement root, out string? trId)
    {
        trId = string.Empty;

        return root.TryGetProperty("header", out var header) &&
               header.TryGetProperty("tr_id", out var trIdElement) &&
               !string.IsNullOrEmpty(trId = trIdElement.GetString());
    }

    private static bool TryGetSubscriptionResponse(JsonElement root, out string? message)
    {
        message = string.Empty;

        if (!root.TryGetProperty("body", out var body))
            return false;

        if (!body.TryGetProperty("rt_cd", out var rtCd) ||
            rtCd.GetString() != "0")
            return false;

        if (!body.TryGetProperty("msg1", out var msg1))
            return false;

        message = msg1.GetString() ?? string.Empty;
        return message == "SUBSCRIBE SUCCESS";
    }
}