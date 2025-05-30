using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Parsers;

/// <summary>
/// 주식 데이터 파서
/// </summary>
public class StockDataParser
{
    private readonly ILogger<StockDataParser> _logger;

    public StockDataParser(ILogger<StockDataParser> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string[]> ParseRecords(string bodyData, int dataCount)
    {
        var allFields = bodyData.Split(KisRealTimeConstants.Parsing.FieldDelimiter);

        if (allFields.Length == 0 || dataCount <= 0)
        {
            _logger.LogWarning("유효하지 않은 데이터: 필드수={FieldCount}, 데이터건수={DataCount}",
                allFields.Length, dataCount);
            yield break;
        }

        var fieldsPerRecord = allFields.Length / dataCount;

        _logger.LogDebug("레코드 파싱 시작 - 전체필드: {TotalFields}, 레코드당필드: {FieldsPerRecord}, 데이터건수: {DataCount}",
            allFields.Length, fieldsPerRecord, dataCount);

        for (var recordIndex = 0; recordIndex < dataCount; recordIndex++)
        {
            var startIndex = recordIndex * fieldsPerRecord;

            if (startIndex >= allFields.Length)
            {
                _logger.LogWarning("레코드 {RecordIndex} 건너뜀 - 인덱스 범위 초과", recordIndex + 1);
                continue;
            }

            var recordFields = allFields.Skip(startIndex).Take(fieldsPerRecord).ToArray();

            if (recordFields.Length < KisRealTimeConstants.Parsing.MinimumFieldsForProcessing)
            {
                _logger.LogWarning("레코드 {RecordIndex} 건너뜀 - 필드 부족: {FieldCount}",
                    recordIndex + 1, recordFields.Length);
                continue;
            }

            yield return recordFields;
        }
    }
}