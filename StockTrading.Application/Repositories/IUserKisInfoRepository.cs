namespace StockTrading.Application.Repositories;

public interface IUserKisInfoRepository
{
    public Task UpdateKisCredentialsAsync(int userId, string appKey, string appSecret, string accountNumber);
    public Task SaveWebSocketTokenAsync(int userId, string approvalKey);
}