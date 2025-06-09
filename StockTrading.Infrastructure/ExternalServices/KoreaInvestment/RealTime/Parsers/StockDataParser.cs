using Microsoft.Extensions.Logging;
using StockTrading.Domain.Settings;
using StockTrading.Domain.Settings.ExternalServices;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.Parsers;

public class StockDataParser
{
    private readonly ILogger<StockDataParser> _logger;
    private readonly RealTimeDataSettings _settings;

    public StockDataParser(ILogger<StockDataParser> logger, RealTimeDataSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public IEnumerable<string[]> ParseRecords(string bodyData, int dataCount)
    {
        var parsing = _settings.Parsing;
        var allFields = bodyData.Split(parsing.FieldDelimiter);

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

            if (recordFields.Length < parsing.MinimumFieldsForProcessing)
            {
                _logger.LogWarning("레코드 {RecordIndex} 건너뜀 - 필드 부족: {FieldCount}",
                    recordIndex + 1, recordFields.Length);
                continue;
            }

            yield return recordFields;
        }
    }
}