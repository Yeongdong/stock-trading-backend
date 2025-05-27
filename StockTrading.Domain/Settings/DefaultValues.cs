namespace StockTrading.Domain.Settings;

public class DefaultValues
{
    public string AccountProductCode { get; set; } = "01";
    public string BalanceTransactionId { get; set; } = "VTTC8434R";
    public string AfterHoursForeignPrice { get; set; } = "N";
    public string OfflineYn { get; set; } = "";
    public string InquiryDivision { get; set; } = "02";
    public string UnitPriceDivision { get; set; } = "01";
    public string FundSettlementInclude { get; set; } = "N";
    public string FinancingAmountAutoRedemption { get; set; } = "N";
    public string ProcessDivision { get; set; } = "00";
}