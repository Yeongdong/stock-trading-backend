namespace StockTrading.DataAccess.DTOs;

public class KisTokenDto
{
    public int Id { get; set; }
    public string AccessToken { get; set; }
    public DateTime ExpiresIn { get; set; }
    public string TokenType { get; set; }
}