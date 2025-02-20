namespace StockTrading.DataAccess.Repositories;

public interface IUserKisInfoRepository
{
    public Task UpdateUserKisInfo(int userId, string appKey, string appSecret, string accountNumber);
    public Task SaveWebSocketTokenAsync(int userId, string approvalKey);
}