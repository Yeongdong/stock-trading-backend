using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Trading.Orders;

public class OrderRequest
{
    [JsonPropertyName("acntPrdtCd")]
    public string ACNT_PRDT_CD { get; set; } = "01";
    
    [JsonPropertyName("trId")]
    [Required(ErrorMessage = "거래구분은 필수입니다.")]
    public string tr_id { get; init; }

    [JsonPropertyName("pdno")]
    [Required(ErrorMessage = "종목코드는 필수입니다.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "종목코드는 6자리 숫자여야 합니다.")]
    public string PDNO { get; init; }
    
    [JsonPropertyName("ordDvsn")]
    [Required(ErrorMessage = "주문구분은 필수입니다.")]
    public string ORD_DVSN { get; init; }
    
    [JsonPropertyName("ordQty")]
    [Required(ErrorMessage = "주문수량은 필수입니다.")]
    public string ORD_QTY { get; init; }
    
    [JsonPropertyName("ordUnpr")]
    [Required(ErrorMessage = "주문단가는 필수입니다.")]
    public string ORD_UNPR { get; set; }
    
    [JsonIgnore]
    public int QuantityAsInt => int.TryParse(ORD_QTY, out var result) ? result : 0;

    [JsonIgnore]
    public decimal PriceAsDecimal => decimal.TryParse(ORD_UNPR, out var result) ? result : 0m;
}