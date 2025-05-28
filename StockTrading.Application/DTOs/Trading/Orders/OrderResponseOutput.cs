namespace StockTrading.Application.DTOs.Trading.Orders;

public class OrderResponseOutput
{
    public string KRX_FWDG_ORD_ORGNO { get; set; } // 거래소코드
    public string ODNO { get; set; } // 주문번호
    public string ORD_TMD { get; set; } // 주문시간
}