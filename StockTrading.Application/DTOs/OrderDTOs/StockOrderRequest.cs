using System.Text.Json.Serialization;

namespace StockTrading.DataAccess.DTOs.OrderDTOs;

public class StockOrderRequest
{
    [JsonPropertyName("acntPrdtCd")]
    public string ACNT_PRDT_CD { get; set; } = "01"; // 계좌상품코드
    
    [JsonPropertyName("trId")]
    public string tr_id { get; set; } // 거래구분

    [JsonPropertyName("pdno")]
    public string PDNO { get; set; } // 종목코드
    
    [JsonPropertyName("ordDvsn")]
    public string ORD_DVSN { get; set; } // 주문구분
    
    [JsonPropertyName("ordQty")]
    public string ORD_QTY { get; set; } // 주문수량
    
    [JsonPropertyName("ordUnpr")]
    public string ORD_UNPR { get; set; } // 주문단가
}