using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading.Converters;

/// <summary>
/// 해외 주식 주문 데이터 변환기
/// </summary>
public class OverseasOrderDataConverter
{
    /// <summary>
    /// KIS 해외 주식 주문 응답을 애플리케이션 응답으로 변환
    /// </summary>
    public OverseasOrderResponse ConvertToOverseasOrderResponse(KisOverseasOrderResponse kisResponse,
        OverseasOrderRequest originalRequest)
    {
        var output = kisResponse.Output;
        var currency = GetCurrencyByMarket(originalRequest.Market);

        return new OverseasOrderResponse
        {
            OrderNumber = output.OrderNumber,
            OrderTime = output.OrderTime,
            StockCode = originalRequest.PDNO,
            StockName = GetStockNameFromCode(originalRequest.PDNO), // TODO: 실제로는 별도 API 호출 필요
            Market = originalRequest.Market,
            TradeType = originalRequest.tr_id,
            OrderDivision = originalRequest.ORD_DVSN,
            Quantity = originalRequest.QuantityAsInt,
            Price = originalRequest.PriceAsDecimal,
            OrderCondition = "DAY",
            Currency = currency,
            OrderStatus = "접수",
            IsSuccess = kisResponse.IsSuccess,
            Message = kisResponse.Message
        };
    }

    /// <summary>
    /// KIS 해외 주식 체결 내역 응답을 애플리케이션 응답으로 변환
    /// </summary>
    public List<OverseasOrderExecution> ConvertToOverseasOrderExecutions(KisOverseasOrderExecutionResponse kisResponse)
    {
        if (!kisResponse.HasData || kisResponse.Output == null)
            return [];

        return kisResponse.Output.Select(ConvertToOverseasOrderExecution).ToList();
    }

    /// <summary>
    /// 개별 체결 내역 데이터 변환
    /// </summary>
    private OverseasOrderExecution ConvertToOverseasOrderExecution(KisOverseasOrderExecutionData data)
    {
        return new OverseasOrderExecution
        {
            ExecutionNumber = GenerateExecutionNumber(data.OrderNumber, data.ExecutionDate, data.ExecutionTime),
            OrderNumber = data.OrderNumber,
            ExecutionTime = ParseExecutionDateTime(data.ExecutionDate, data.ExecutionTime),
            StockCode = data.StockCode,
            StockName = data.StockName,
            Market = GetMarketFromCurrency(data.CurrencyCode),
            TradeType = ConvertTradeTypeCode(data.TradeTypeCode),
            ExecutedQuantity = ParseIntegerSafely(data.ExecutedQuantity),
            ExecutedPrice = ParseDecimalSafely(data.ExecutedPrice),
            ExecutedAmount = ParseDecimalSafely(data.ExecutedAmount),
            Currency = data.CurrencyCode,
            Commission = CalculateCommission(ParseDecimalSafely(data.ExecutedAmount)), // TODO: 실제 수수료 계산 로직 필요
            Tax = CalculateTax(ParseDecimalSafely(data.ExecutedAmount)), // TODO: 실제 세금 계산 로직 필요
            ExchangeRate = ParseDecimalSafely(data.ExchangeRate)
        };
    }

    #region Helper Methods

    /// <summary>
    /// 시장별 통화 반환
    /// </summary>
    private string GetCurrencyByMarket(StockTrading.Domain.Enums.Market market)
    {
        return market switch
        {
            Domain.Enums.Market.Nasdaq or Domain.Enums.Market.Nyse => "USD",
            Domain.Enums.Market.Tokyo => "JPY",
            Domain.Enums.Market.London => "GBP",
            Domain.Enums.Market.Hongkong => "HKD",
            _ => "USD"
        };
    }

    /// <summary>
    /// 종목코드로부터 종목명 조회 (임시 구현)
    /// </summary>
    private string GetStockNameFromCode(string stockCode)
    {
        // 실제로는 종목명 조회 API 호출이 필요
        // 임시로 종목코드 반환
        return stockCode;
    }

    /// <summary>
    /// 통화코드로부터 시장 구분 추정
    /// </summary>
    private StockTrading.Domain.Enums.Market GetMarketFromCurrency(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => Domain.Enums.Market.Nasdaq, // 기본값으로 나스닥 설정
            "JPY" => Domain.Enums.Market.Tokyo,
            "GBP" => Domain.Enums.Market.London,
            "HKD" => Domain.Enums.Market.Hongkong,
            _ => Domain.Enums.Market.Nasdaq
        };
    }

    /// <summary>
    /// 거래 구분 코드 변환
    /// </summary>
    private string ConvertTradeTypeCode(string tradeTypeCode)
    {
        return tradeTypeCode switch
        {
            "02" => "VTTT1002U", // 매수
            "01" => "VTTT1001U", // 매도
            _ => tradeTypeCode
        };
    }

    /// <summary>
    /// 체결번호 생성
    /// </summary>
    private string GenerateExecutionNumber(string orderNumber, string executionDate, string executionTime)
    {
        return $"{orderNumber}_{executionDate}_{executionTime}";
    }

    /// <summary>
    /// 체결일시 파싱
    /// </summary>
    private DateTime ParseExecutionDateTime(string executionDate, string executionTime)
    {
        try
        {
            // executionDate: YYYYMMDD, executionTime: HHMMSS
            var dateString = $"{executionDate} {executionTime}";
            return DateTime.ParseExact(dateString, "yyyyMMdd HHmmss", null);
        }
        catch
        {
            return DateTime.UtcNow;
        }
    }

    private int ParseIntegerSafely(string value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }

    private decimal ParseDecimalSafely(string value)
    {
        return decimal.TryParse(value, out var result) ? result : 0m;
    }

    /// <summary>
    /// 수수료 계산 (임시 구현)
    /// </summary>
    private decimal CalculateCommission(decimal executedAmount)
    {
        // 실제 해외 주식 수수료 계산 로직 필요
        // 임시로 0.1% 적용
        return executedAmount * 0.001m;
    }

    /// <summary>
    /// 세금 계산 (임시 구현)
    /// </summary>
    private decimal CalculateTax(decimal executedAmount)
    {
        // 실제 해외 주식 세금 계산 로직 필요
        // 임시로 0.05% 적용
        return executedAmount * 0.0005m;
    }

    #endregion
}