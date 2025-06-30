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
    /// KIS 예약주문 응답을 도메인 모델로 변환
    /// </summary>
    public OverseasOrderResponse ConvertToScheduledOrderResponse(KisScheduledOverseasOrderResponse kisResponse,
        ScheduledOverseasOrderRequest request)
    {
        // 미국 주문과 아시아 주문에 따라 주문번호 필드가 다름
        var orderNumber = kisResponse.Output?.OrderNumber ??
                          kisResponse.Output?.OverseasReservedOrderNumber ?? string.Empty;

        return new OverseasOrderResponse
        {
            OrderNumber = orderNumber,
            OrderTime = DateTime.Now.ToString("HHmmss"),
            StockCode = request.PDNO,
            Market = GetMarketFromExchangeCode(request.OVRS_EXCG_CD),
            TradeType = GetTradeTypeName(request.tr_id),
            OrderDivision = "예약주문",
            Quantity = int.Parse(request.ORD_QTY),
            Price = decimal.Parse(request.OVRS_ORD_UNPR),
            Currency = GetCurrencyFromExchangeCode(request.OVRS_EXCG_CD),
            OrderStatus = "예약접수",
            IsSuccess = kisResponse.IsSuccess,
            Message = kisResponse.Message
        };
    }

    /// <summary>
    /// KIS 해외 주식 체결 내역 응답을 애플리케이션 응답으로 변환
    /// </summary>
    public List<OverseasOrderExecution> ConvertToOverseasOrderExecutions(KisOverseasOrderExecutionResponse kisResponse)
    {
        if (!kisResponse.IsSuccess || kisResponse.Output == null || kisResponse.Output.Count == 0)
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
            ExecutionNumber = GenerateExecutionNumber(data.OrderNumber, data.OrderDate, data.OrderTime),
            OrderNumber = data.OrderNumber,
            ExecutionTime = ParseExecutionDateTime(data.OrderDate, data.OrderTime),
            StockCode = data.StockCode,
            StockName = data.StockName,
            Market = GetMarketFromCurrency(data.CurrencyCode),
            TradeType = ConvertTradeTypeCode(data.TradeTypeCode),
            ExecutedQuantity = ParseIntegerSafely(data.ExecutedQuantity),
            ExecutedPrice = ParseDecimalSafely(data.ExecutedPrice),
            ExecutedAmount = ParseDecimalSafely(data.ExecutedAmount),
            Currency = data.CurrencyCode,
            Commission = CalculateCommission(ParseDecimalSafely(data.ExecutedAmount)),
            Tax = CalculateTax(ParseDecimalSafely(data.ExecutedAmount)),
            ExchangeRate = 1.0m // 환율 정보는 별도 API 호출 필요
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

    private StockTrading.Domain.Enums.Market GetMarketFromExchangeCode(string exchangeCode)
    {
        return exchangeCode switch
        {
            "NASD" => Domain.Enums.Market.Nasdaq,
            "NYSE" => Domain.Enums.Market.Nyse,
            "AMEX" => Domain.Enums.Market.Nyse, // AMEX는 NYSE로 처리
            "SEHK" => Domain.Enums.Market.Hongkong,
            "TKSE" => Domain.Enums.Market.Tokyo,
            // "SHAA" => Domain.Enums.Market.Shanghai,
            // "SZAA" => Domain.Enums.Market.Shenzhen,
            _ => Domain.Enums.Market.Nasdaq
        };
    }

    private string GetTradeTypeName(string tradeType)
    {
        return tradeType switch
        {
            "VTTT1002U" or "VTTT3014U" => "매수",
            "VTTT1001U" or "VTTT3016U" => "매도",
            "VTTS3013U" => "예약주문",
            _ => "알 수 없음"
        };
    }

    private string GetOrderDivisionName(string orderDivision)
    {
        return orderDivision switch
        {
            "00" => "지정가",
            "01" => "시장가",
            "31" => "MOO(장개시시장가)",
            _ => "알 수 없음"
        };
    }

    private string GetCurrencyFromExchangeCode(string exchangeCode)
    {
        return exchangeCode switch
        {
            "NASD" or "NYSE" or "AMEX" => "USD",
            "SEHK" => "HKD",
            "TKSE" => "JPY",
            "SHAA" or "SZAA" => "CNY",
            _ => "USD"
        };
    }

    #endregion
}