namespace stock_trading_backend.DTOs;

public class UserInfoRequest
{
    public string AppKey { get; set; }
    public string AppSecret { get; set; }
    public string AccountNumber { get; set; }
}