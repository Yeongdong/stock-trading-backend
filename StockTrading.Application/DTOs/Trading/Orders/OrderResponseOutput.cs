using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Trading.Orders;

public class OrderResponseOutput
{
    [JsonPropertyName("KRX_FWDG_ORD_ORGNO")]
    public string KrxForwardOrderOrgNo { get; set; } // 거래소코드
    
    [JsonPropertyName("ODNO")]
    public string OrderNumber { get; set; }  // 주문번호
    
    [JsonPropertyName("ORD_TMD")]
    public string OrderTime { get; set; } // 주문시간
}