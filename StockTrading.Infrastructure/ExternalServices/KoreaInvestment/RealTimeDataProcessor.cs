using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Parsers;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/// <summary>
/// 수신된 메시지 처리 및 파싱
/// </summary>
public class RealTimeDataProcessor : IRealTimeDataProcessor
{
    private readonly ILogger<RealTimeDataProcessor> _logger;
    private readonly RealTimeDataSettings _settings;
    private readonly JsonMessageParser _jsonParser;
    private readonly PipeDelimitedMessageParser _pipeParser;
    private readonly StockDataParser _stockDataParser;
    private readonly StockDataConverter _stockDataConverter;

    public event EventHandler<KisTransactionInfo> StockPriceReceived;
    public event EventHandler<object> TradeExecutionReceived;

    public RealTimeDataProcessor(ILogger<RealTimeDataProcessor> logger, ILoggerFactory loggerFactory,
        IOptions<RealTimeDataSettings> settings, StockDataConverter stockDataConverter)
    {
        _logger = logger;
        _settings = settings.Value;
        _stockDataConverter = stockDataConverter;

        _jsonParser = new JsonMessageParser(loggerFactory.CreateLogger<JsonMessageParser>(), _settings);
        _pipeParser =
            new PipeDelimitedMessageParser(loggerFactory.CreateLogger<PipeDelimitedMessageParser>(), _settings);
        _stockDataParser = new StockDataParser(loggerFactory.CreateLogger<StockDataParser>(), _settings);
    }

    public void ProcessMessage(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText)) return;

        var parseResult = DetermineMessageTypeAndParse(messageText);
        ProcessParsedMessage(parseResult);
    }

    private MessageParseResult DetermineMessageTypeAndParse(string messageText)
    {
        var trimmedMessage = messageText.TrimStart();
        var jsonStartPattern = "{";

        return trimmedMessage.StartsWith(jsonStartPattern)
            ? _jsonParser.Parse(messageText)
            : _pipeParser.Parse(messageText);
    }

    private void ProcessParsedMessage(MessageParseResult parseResult)
    {
        var messageTypes = _settings.MessageTypes;

        if (parseResult.TrId == messageTypes.StockExecution)
            ProcessStockExecutionData(parseResult.Data!, parseResult.DataCount);
        else if (parseResult.TrId == messageTypes.StockAskBid)
            _logger.LogDebug("주식 호가 데이터 수신");
        else if (parseResult.TrId == messageTypes.TradeNotification ||
                 parseResult.TrId == messageTypes.TradeNotificationDemo)
            ProcessTradeExecutionData(parseResult.Data!);
        else if (parseResult.TrId == messageTypes.PingPong)
            _logger.LogTrace("핑퐁 메시지 수신");
        else
            _logger.LogDebug("알 수 없는 TR ID: {TrId}", parseResult.TrId);
    }

    private void ProcessStockExecutionData(string bodyData, int dataCount)
    {
        var processedCount = 0;

        _logger.LogInformation("[DataProcessor] 주식 체결 데이터 처리 시작: 총 {DataCount}건", dataCount);

        foreach (var recordFields in _stockDataParser.ParseRecords(bodyData, dataCount))
        {
            var transactionInfo = _stockDataConverter.ConvertToTransactionInfo(recordFields, processedCount + 1);

            _logger.LogInformation("[DataProcessor] 주가 데이터 변환 완료: {Symbol} - {Price}원 ({ChangeType})",
                transactionInfo.Symbol, transactionInfo.Price, transactionInfo.ChangeType);

            // 이벤트 발생 - 여기서 브로드캐스터가 호출됨
            StockPriceReceived?.Invoke(this, transactionInfo);

            _logger.LogInformation("[DataProcessor] StockPriceReceived 이벤트 발생: {Symbol}",
                transactionInfo.Symbol);

            processedCount++;
        }

        _logger.LogInformation("[DataProcessor] 주식 체결 데이터 처리 완료: {ProcessedCount}/{TotalCount}",
            processedCount, dataCount);
    }

    private void ProcessTradeExecutionData(string bodyData)
    {
        // 체결 통보 데이터는 현재 구현에서 단순 로깅만 수행
        // 필요시 향후 구현 확장
        _logger.LogInformation("체결 통보 데이터 수신: {Data}", bodyData);

        var executionData = new { Data = bodyData, ProcessedTime = DateTime.Now };
        TradeExecutionReceived?.Invoke(this, executionData);
    }
}