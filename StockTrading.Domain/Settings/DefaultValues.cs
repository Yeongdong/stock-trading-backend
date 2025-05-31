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
    /// </summary>
    public string OrderExecutionTransactionId { get; set; } = "TTTC8001R";
    public string OrderExecutionInquiryDivision { get; set; } = "00";
    public string OrderExecutionSettlementDivision { get; set; } = "01"; // 01:체결, 02:미체결
    public string OrderExecutionInquiryDivision3 { get; set; } = "00";
    
    /// <summary>
    /// 매매구분 코드들
    /// </summary>
    public string SellOrderCode { get; set; } = "01";  // 매도
    public string BuyOrderCode { get; set; } = "02";   // 매수
    public string AllOrderCode { get; set; } = "00";   // 전체
}