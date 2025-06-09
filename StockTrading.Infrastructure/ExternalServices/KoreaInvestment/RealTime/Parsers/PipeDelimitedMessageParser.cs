using Microsoft.Extensions.Logging;
using StockTrading.Domain.Settings;
using StockTrading.Domain.Settings.ExternalServices;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.Parsers;

public class PipeDelimitedMessageParser
{
    private readonly ILogger<PipeDelimitedMessageParser> _logger;
    private readonly RealTimeDataSettings _settings;

    public PipeDelimitedMessageParser(ILogger<PipeDelimitedMessageParser> logger, RealTimeDataSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public MessageParseResult Parse(string messageText)
    {
        var parsing = _settings.Parsing;
        string?[] parts = messageText.Split(parsing.PipeDelimiter);

        if (parts.Length < parsing.MinimumPipeSegments)
        {
            return MessageParseResult.Failure($"메시지 형식이 올바르지 않음. 세그먼트 수: {parts.Length}");
        }

        var trId = parts[1];
        var dataCountStr = parts[2];
        var bodyData = parts[3];

        if (!int.TryParse(dataCountStr, out var dataCount))
        {
            return MessageParseResult.Failure($"데이터 건수 파싱 실패: {dataCountStr}");
        }

        _logger.LogDebug("파이프 메시지 파싱 성공 - TrId: {TrId}, 데이터 건수: {DataCount}", trId, dataCount);

        return MessageParseResult.Success(trId, bodyData, dataCount);
    }
}
