namespace StockTrading.Domain.Entities;

public class KisToken
{
    public int Id { get; set; }
    public string AccessToken { get; set; }
    public DateTime ExpiresIn { get; set; }
    public string TokenType { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
}