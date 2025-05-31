using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Constants;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Parsers;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/// <summary>
/// 수신된 메시지 처리 및 파싱
/// </summary>
public class RealTimeDataProcessor : IRealTimeDataProcessor
{
    private readonly ILogger<RealTimeDataProcessor> _logger;
    private readonly JsonMessageParser _jsonParser;
    private readonly PipeDelimitedMessageParser _pipeParser;
    private readonly StockDataParser _stockDataParser;
    private readonly StockDataConverter _stockDataConverter;

    public event EventHandler<KisTransactionInfo> StockPriceReceived;
    public event EventHandler<object> TradeExecutionReceived;

    public RealTimeDataProcessor(ILogger<RealTimeDataProcessor> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _jsonParser = new JsonMessageParser(loggerFactory.CreateLogger<JsonMessageParser>());
        _pipeParser = new PipeDelimitedMessageParser(loggerFactory.CreateLogger<PipeDelimitedMessageParser>());
        _stockDataParser = new StockDataParser(loggerFactory.CreateLogger<StockDataParser>());
        _stockDataConverter = new StockDataConverter(loggerFactory.CreateLogger<StockDataConverter>());
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

        return trimmedMessage.StartsWith(KisRealTimeConstants.Parsing.JsonStartPattern)
            ? _jsonParser.Parse(messageText)
            : _pipeParser.Parse(messageText);
    }

    private void ProcessParsedMessage(MessageParseResult parseResult)
    {
        switch (parseResult.TrId)
        {
            case KisRealTimeConstants.MessageTypes.StockExecution:
                ProcessStockExecutionData(parseResult.Data!, parseResult.DataCount);
                break;

            case KisRealTimeConstants.MessageTypes.StockAskBid:
                _logger.LogDebug("주식 호가 데이터 수신");
                break;

            case KisRealTimeConstants.MessageTypes.TradeNotification:
            case KisRealTimeConstants.MessageTypes.TradeNotificationDemo:
                ProcessTradeExecutionData(parseResult.Data!);
                break;

            case KisRealTimeConstants.MessageTypes.PingPong:
                _logger.LogTrace("핑퐁 메시지 수신");
                break;

            default:
                _logger.LogDebug("알 수 없는 TR ID: {TrId}", parseResult.TrId);
                break;
        }
    }

    private void ProcessStockExecutionData(string bodyData, int dataCount)
    {
        var processedCount = 0;

        foreach (var recordFields in _stockDataParser.ParseRecords(bodyData, dataCount))
        {
            var transactionInfo = _stockDataConverter.ConvertToTransactionInfo(recordFields, processedCount + 1);

            if (transactionInfo != null)
            {
                _logger.LogInformation("주가 데이터 발생: {Symbol} - {Price}원 ({ChangeType})",
                    transactionInfo.Symbol, transactionInfo.Price, transactionInfo.ChangeType);

                StockPriceReceived?.Invoke(this, transactionInfo);
            }

            processedCount++;
        }

        _logger.LogDebug("주식 체결 데이터 처리 완료: {ProcessedCount}/{TotalCount}", processedCount, dataCount);
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