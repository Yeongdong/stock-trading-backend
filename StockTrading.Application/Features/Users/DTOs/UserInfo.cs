using StockTrading.Domain.Enums;

namespace StockTrading.Application.Features.Users.DTOs;

public class UserInfo
{
    public int Id { get; init; }
    public string? Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public UserRole Role { get; init; }
    public string? KisAppKey { get; set; }
    public string? KisAppSecret { get; set; }
    public string? AccountNumber { get; set; }
    public KisTokenInfo? KisToken { get; set; }
    public string? WebSocketToken { get; init; }
}