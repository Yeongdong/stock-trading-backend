namespace StockTrading.DataAccess.Services.Interfaces;

public interface IKisService
{
    Task SaveTokenAsync(int userId, string accessToken, DateTime expiresIn, string tokenType);
}