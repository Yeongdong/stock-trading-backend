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
        return trimmedMessage.StartsWith("{")
            ? _jsonParser.Parse(messageText)
            : _pipeParser.Parse(messageText);
    }

    private void ProcessParsedMessage(MessageParseResult parseResult)
    {
        var messageTypes = _settings.MessageTypes;

        if (parseResult.TrId == messageTypes.StockExecution)
            ProcessStockExecutionData(parseResult.Data!, parseResult.DataCount);
        else if (parseResult.TrId == messageTypes.TradeNotification ||
                 parseResult.TrId == messageTypes.TradeNotificationDemo)
            ProcessTradeExecutionData(parseResult.Data!);
    }

    private void ProcessStockExecutionData(string bodyData, int dataCount)
    {
        var processedCount = 0;

        foreach (var recordFields in _stockDataParser.ParseRecords(bodyData, dataCount))
        {
            var transactionInfo = _stockDataConverter.ConvertToTransactionInfo(recordFields, processedCount + 1);
            if (transactionInfo == null) continue;
            _logger.LogInformation("주가 데이터: {Symbol} {Price}원", transactionInfo.Symbol, transactionInfo.Price);

            StockPriceReceived?.Invoke(this, transactionInfo);
            processedCount++;
        }
    }

    private void ProcessTradeExecutionData(string bodyData)
    {
        _logger.LogInformation("체결 통보 데이터 수신: {Data}", bodyData);
        var executionData = new { Data = bodyData, ProcessedTime = DateTime.Now };
        TradeExecutionReceived?.Invoke(this, executionData);
    }
}