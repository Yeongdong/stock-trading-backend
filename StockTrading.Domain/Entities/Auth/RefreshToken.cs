namespace StockTrading.Domain.Entities.Auth;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}