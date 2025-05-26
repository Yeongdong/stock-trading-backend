namespace StockTrading.Application.DTOs.Orders;

public class OrderOutput
{
    public string KRX_FWDG_ORD_ORGNO { get; set; }  // 한국거래소전송주문조직번호
    public string ODNO { get; set; }                // 주문번호
    public string ORD_TMD { get; set; }             // 주문시각
}