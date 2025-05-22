using Microsoft.Extensions.Configuration;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 테스트용 데이터 생성을 담당하는 팩토리 클래스
/// </summary>
public class TestDataFactory
{
    private readonly IConfiguration _configuration;

    public TestDataFactory(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// 테스트용 사용자 생성
    /// </summary>
    public User CreateTestUser()
    {
        var userConfig = _configuration.GetSection("TestData:User");

        return new User
        {
            Email = userConfig["Email"] ?? "test@example.com",
            Name = userConfig["Name"] ?? "Test User",
            GoogleId = userConfig["GoogleId"] ?? "test_google_id",
            Role = userConfig["Role"] ?? "User",
            CreatedAt = DateTime.UtcNow,
            KisAppKey = userConfig["KisAppKey"] ?? "test_app_key",
            KisAppSecret = userConfig["KisAppSecret"] ?? "test_app_secret",
            AccountNumber = userConfig["AccountNumber"] ?? "1234567890",
            WebSocketToken = userConfig["WebSocketToken"] ?? "test_websocket_token"
        };
    }

    /// <summary>
    /// 테스트용 KIS 토큰 생성
    /// </summary>
    public KisToken CreateTestKisToken(int userId)
    {
        var userConfig = _configuration.GetSection("TestData:User");
        var tokenExpirationHours = userConfig.GetValue<int>("TokenExpirationHours", 1);

        return new KisToken
        {
            UserId = userId,
            AccessToken = userConfig["KisAccessToken"] ?? "test_kis_access_token",
            ExpiresIn = DateTime.UtcNow.AddHours(tokenExpirationHours),
            TokenType = userConfig["TokenType"] ?? "Bearer"
        };
    }

    /// <summary>
    /// 여러 테스트 사용자 생성
    /// </summary>
    public List<User> CreateMultipleTestUsers(int count)
    {
        var users = new List<User>();

        for (int i = 0; i < count; i++)
        {
            var user = CreateTestUser();
            user.Email = $"test{i + 1}@example.com";
            user.GoogleId = $"test_google_id_{i + 1}";
            users.Add(user);
        }

        return users;
    }
}