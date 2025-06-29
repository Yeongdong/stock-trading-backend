namespace StockTrading.Domain.Entities.Auth;

public class RefreshToken
{
    public int Id { get; init; }
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    public int UserId { get; init; }
    public User User { get; init; } = null!;
    
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}