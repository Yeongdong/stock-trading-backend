using StockTrading.Domain.Enums;

namespace StockTrading.Domain.Entities;

public class User
{
    public int Id { get; init; }
    public string Email { get; init; } = null!;
    public string Name { get; set; } = null!;
    public string GoogleId { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public UserRole Role { get; init; } = UserRole.User;
    public string? PasswordHash { get; init; }
    public string? KisAppKey { get; set; }
    public string? KisAppSecret { get; set; }
    public string? AccountNumber { get; set; }
    public KisToken? KisToken { get; init; }
    public string? WebSocketToken { get; set; }
    public decimal? PreviousDayTotalAmount { get; set; }
}