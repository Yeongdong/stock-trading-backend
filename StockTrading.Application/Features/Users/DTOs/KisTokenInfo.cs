using StockTrading.Domain.Entities;

namespace StockTrading.Application.Features.Users.DTOs;

public class KisTokenInfo
{
    public int Id { get; set; }
    public string AccessToken { get; set; }
    public DateTime ExpiresIn { get; set; }
    public string TokenType { get; set; }

    public KisToken ToEntity()
    {
        return new KisToken
        {
            Id = Id,
            AccessToken = AccessToken,
            ExpiresIn = ExpiresIn,
            TokenType = TokenType,
        };
    }
}