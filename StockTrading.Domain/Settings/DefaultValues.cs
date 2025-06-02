namespace StockTrading.Domain.Settings;

public class DefaultValues
{
    public string AccountProductCode { get; init; } = "01";
    public string BalanceTransactionId { get; init; } = "VTTC8434R";
    public string AfterHoursForeignPrice { get; init; } = "N";
    public string OfflineYn { get; init; } = "";
    public string InquiryDivision { get; init; } = "02";
    public string UnitPriceDivision { get; init; } = "01";
    public string FundSettlementInclude { get; init; } = "N";
    public string FinancingAmountAutoRedemption { get; init; } = "N";
    public string ProcessDivision { get; init; } = "00";
    
    /// <summary>
    /// 주문체결조회 관련 기본값들
    /// 모의투자: VTTC0081R (3개월이내) 또는 VTSC9215R (3개월이전)
    /// 실전투자: TTTC0081R (3개월이내) 또는 CTSC9215R (3개월이전)
    /// </summary>
    public string OrderExecutionTransactionId { get; set; } = "VTTC0081R"; 
    
    /// <summary>
    /// 주식 현재가 조회 TR ID
    /// 모의투자: FHKST01010100
    /// 실전투자: FHKST01010100
    /// </summary>
    public string CurrentPriceTransactionId { get; set; } = "FHKST01010100";
    
    /// <summary>
    /// 매매구분 코드들
    /// </summary>
    public string SellOrderCode { get; set; } = "01";  // 매도
    public string BuyOrderCode { get; set; } = "02";   // 매수
    public string AllOrderCode { get; set; } = "00";   // 전체
    
    /// <summary>
    /// 매수가능 조회 코드
    /// </summary>
    public string BuyableInquiryTransactionId { get; set; } = "VTTC8908R";
}