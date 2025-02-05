namespace stock_trading_backend;

public class TokenResponse
{
    public string access_token { get; }
    public string token_type { get; }
    public int expires_in { get; }
}