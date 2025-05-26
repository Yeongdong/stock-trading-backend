using StockTrading.Application.DTOs.Common;

namespace StockTrading.Application.Repositories;

public interface IKisTokenRepository
{
    public Task SaveKisToken(int userId, TokenResponse tokenResponse);
}