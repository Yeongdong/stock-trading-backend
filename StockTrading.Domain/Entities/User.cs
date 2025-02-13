namespace StockTradingBackend.DataAccess.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string GoogleId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = null!;
    public string? PasswordHash { get; set; }
    public string? KisAppKey { get; set; }
    public string? KisAppSecret { get; set; }
    public string? AccountNumber { get; set; }
    public KisToken? KisToken { get; set; }
}