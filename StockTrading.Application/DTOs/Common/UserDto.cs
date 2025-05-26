using StockTrading.Domain.Entities;

namespace StockTrading.Application.DTOs.Common;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? KisAppKey { get; set; }
    public string? KisAppSecret { get; set; }
    public string? AccountNumber { get; set; }
    public KisTokenDto? KisToken { get; set; }
    public string? WebSocketToken { get; set; }

    public User ToEntity()
    {
        return new User
        {
            Id = Id,
            Email = Email,
            Name = Name,
            KisAppKey = KisAppKey,
            KisAppSecret = KisAppSecret,
            AccountNumber = AccountNumber,
            KisToken = KisToken?.ToEntity(),
            WebSocketToken = WebSocketToken,
        };
    }
}