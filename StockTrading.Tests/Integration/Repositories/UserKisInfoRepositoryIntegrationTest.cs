using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Infrastructure.Security.Encryption;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Repositories;

public class UserKisInfoRepositoryIntegrationTest
{
    private ApplicationDbContext CreateContext(string dbName)
    {
        var mockEncryptionService = new Mock<IEncryptionService>();
        mockEncryptionService.Setup(s => s.Encrypt(It.IsAny<string>()))
            .Returns<string>(input => input);
        mockEncryptionService.Setup(s => s.Decrypt(It.IsAny<string>()))
            .Returns<string>(input => input);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options, mockEncryptionService.Object);
    }

    [Fact]
    public async Task UpdateUserKisInfo_ShouldUpdateUserKisInformation()
    {
        string dbName = $"UpdateTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository = new UserKisInfoRepository(context, logger);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "test_google_id",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        int userId = user.Id;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "12345678901234";

        await repository.UpdateUserKisInfo(userId, appKey, appSecret, accountNumber);

        var updatedUser = await context.Users.FindAsync(userId);

        Assert.NotNull(updatedUser);
        Assert.Equal(appKey, updatedUser.KisAppKey);
        Assert.Equal(appSecret, updatedUser.KisAppSecret);
        Assert.Equal(accountNumber, updatedUser.AccountNumber);
    }

    [Fact]
    public async Task UpdateUserKisInfo_WithNonExistentUser_ShouldThrowKeyNotFoundException()
    {
        string dbName = $"NonExistentUserTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository = new UserKisInfoRepository(context, logger);

        int nonExistentUserId = 9999;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "12345678901234";

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.UpdateUserKisInfo(nonExistentUserId, appKey, appSecret, accountNumber));
    }

    [Fact]
    public async Task SaveWebSocketTokenAsync_ShouldUpdateUserWebSocketToken()
    {
        string dbName = $"WebSocketTokenTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository = new UserKisInfoRepository(context, logger);

        var user = new User
        {
            Email = "websocket@example.com",
            Name = "WebSocket Test",
            GoogleId = "websocket_google_id",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        int userId = user.Id;
        string webSocketToken = "test_websocket_token";

        await repository.SaveWebSocketTokenAsync(userId, webSocketToken);

        var updatedUser = await context.Users.FindAsync(userId);

        Assert.NotNull(updatedUser);
        Assert.Equal(webSocketToken, updatedUser.WebSocketToken);
    }

    [Fact]
    public async Task SaveWebSocketTokenAsync_WithNonExistentUser_ShouldThrowKeyNotFoundException()
    {
        string dbName = $"WebSocketNonExistentUserTest_{Guid.NewGuid()}";
        using var context = CreateContext(dbName);
        var logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository = new UserKisInfoRepository(context, logger);

        int nonExistentUserId = 9999;
        string webSocketToken = "test_websocket_token";

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => repository.SaveWebSocketTokenAsync(nonExistentUserId, webSocketToken));
    }

    [Fact]
    public async Task UpdateUserKisInfo_ShouldPersistChanges()
    {
        string dbName = $"PersistChangesTestDb_{Guid.NewGuid()}";
    
        // 첫 번째 컨텍스트: 사용자 생성 및 업데이트
        using var context1 = CreateContext(dbName);
        var logger1 = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository1 = new UserKisInfoRepository(context1, logger1);

        var user = new User
        {
            Email = "transaction@example.com",
            Name = "Transaction Test",
            GoogleId = "transaction_google_id",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        context1.Users.Add(user);
        await context1.SaveChangesAsync();

        int userId = user.Id;
        string appKey = "transaction_app_key";
        string appSecret = "transaction_app_secret";
        string accountNumber = "98765432109876";

        // 업데이트 수행
        await repository1.UpdateUserKisInfo(userId, appKey, appSecret, accountNumber);
    
        // 첫 번째 컨텍스트를 명시적으로 dispose
        await context1.DisposeAsync();

        // 두 번째 컨텍스트: 변경 사항이 실제로 저장되었는지 확인
        using var context2 = CreateContext(dbName);
        var updatedUser = await context2.Users.FindAsync(userId);

        Assert.NotNull(updatedUser);
        Assert.Equal(appKey, updatedUser.KisAppKey);
        Assert.Equal(appSecret, updatedUser.KisAppSecret);
        Assert.Equal(accountNumber, updatedUser.AccountNumber);
    }

    [Fact]
    public async Task UpdateUserKisInfo_MultipleConcurrentUpdates_ShouldUpdateUser()
    {
        string dbName = $"ConcurrentUpdatesTestDb_{Guid.NewGuid()}";

        // 첫 번째 업데이트
        using var context1 = CreateContext(dbName);
        var logger1 = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository1 = new UserKisInfoRepository(context1, logger1);

        var user = new User
        {
            Email = "concurrent@example.com",
            Name = "Concurrent Test",
            GoogleId = "concurrent_google_id",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        context1.Users.Add(user);
        await context1.SaveChangesAsync();
        int userId = user.Id;

        // 첫 번째 업데이트 수행
        await repository1.UpdateUserKisInfo(userId, "app_key_1", "app_secret_1", "account_1");
    
        // context1을 dispose하여 변경사항이 확실히 저장되도록 함
        await context1.DisposeAsync();

        // 두 번째 업데이트 (새로운 컨텍스트)
        using var context2 = CreateContext(dbName);
        var logger2 = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository2 = new UserKisInfoRepository(context2, logger2);

        await repository2.UpdateUserKisInfo(userId, "app_key_2", "app_secret_2", "account_2");
        await context2.DisposeAsync();

        // 검증용 새로운 컨텍스트
        using var context3 = CreateContext(dbName);
        var finalUser = await context3.Users.FindAsync(userId);

        Assert.NotNull(finalUser);
        Assert.Equal("app_key_2", finalUser.KisAppKey);
        Assert.Equal("app_secret_2", finalUser.KisAppSecret);
        Assert.Equal("account_2", finalUser.AccountNumber);
    }
}