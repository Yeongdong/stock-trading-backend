namespace StockTradingBackend.DataAccess.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string GoogleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; }
    public string PasswordHash { get; set; }
    public string KisAppKey { get; set; }
    public string KisAppSecret { get; set; }
    public string AccountNumber { get; set; }
    public KisToken KisToken { get; set; }
}