using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Parsers;

/// <summary>
/// JSON 메시지 파서
/// </summary>
public class JsonMessageParser
{
    private readonly ILogger<JsonMessageParser> _logger;

    public JsonMessageParser(ILogger<JsonMessageParser> logger)
    {
        _logger = logger;
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

        return root.TryGetProperty(KisRealTimeConstants.JsonProperties.Header, out var header) &&
               header.TryGetProperty(KisRealTimeConstants.JsonProperties.TrId, out var trIdElement) &&
               !string.IsNullOrEmpty(trId = trIdElement.GetString());
    }

    private static bool TryGetSubscriptionResponse(JsonElement root, out string? message)
    {
        message = string.Empty;

        if (!root.TryGetProperty(KisRealTimeConstants.JsonProperties.Body, out var body))
            return false;

        if (!body.TryGetProperty(KisRealTimeConstants.JsonProperties.ReturnCode, out var rtCd) ||
            rtCd.GetString() != KisRealTimeConstants.JsonProperties.SuccessCode)
            return false;

        if (!body.TryGetProperty(KisRealTimeConstants.JsonProperties.Message, out var msg1))
            return false;

        message = msg1.GetString() ?? string.Empty;
        return message == KisRealTimeConstants.Parsing.SubscribeSuccessMessage;
    }
}