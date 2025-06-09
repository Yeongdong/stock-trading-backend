namespace StockTrading.Application.Features.Users.DTOs;

public class UserInfo
{
    public int Id { get; init; }
    public string Email { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? KisAppKey { get; set; }
    public string? KisAppSecret { get; set; }
    public string? AccountNumber { get; set; }
    public KisTokenInfo? KisToken { get; set; }
    public string? WebSocketToken { get; init; }
}