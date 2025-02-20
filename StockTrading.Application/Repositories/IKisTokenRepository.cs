using StockTrading.DataAccess.DTOs;

namespace StockTrading.DataAccess.Repositories;

public interface IKisTokenRepository
{
    public Task SaveKisToken(int userId, TokenResponse tokenResponse);
}