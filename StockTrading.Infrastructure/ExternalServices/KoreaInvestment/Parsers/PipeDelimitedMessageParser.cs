using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Parsers;

/// <summary>
/// 파이프 구분 메시지 파서
/// </summary>
public class PipeDelimitedMessageParser
{
    private readonly ILogger<PipeDelimitedMessageParser> _logger;

    public PipeDelimitedMessageParser(ILogger<PipeDelimitedMessageParser> logger)
    {
        _logger = logger;
    }

    public MessageParseResult Parse(string messageText)
    {
        string?[] parts = messageText.Split(KisRealTimeConstants.Parsing.PipeDelimiter);

        if (parts.Length < KisRealTimeConstants.Parsing.MinimumPipeSegments)
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