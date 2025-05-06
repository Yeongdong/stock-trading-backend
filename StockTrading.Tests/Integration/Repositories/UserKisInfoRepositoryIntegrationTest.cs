using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Repositories;

public class UserKisInfoRepositoryIntegrationTest
{
    [Fact]
    public async Task UpdateUserKisInfo_ShouldUpdateUserKisInformation()
    {
        string dbName = $"UpdateTest_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context = new ApplicationDbContext(options);
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
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context = new ApplicationDbContext(options);
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
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context = new ApplicationDbContext(options);
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
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context = new ApplicationDbContext(options);
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
        string dbName = "PersistChangesTestDb";
        
        var options1 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context1 = new ApplicationDbContext(options1);
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
        
        await repository1.UpdateUserKisInfo(userId, appKey, appSecret, accountNumber);
        
        // 새 컨텍스트를 생성하여 변경 사항이 DB에 저장되었는지 확인
        var options2 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context2 = new ApplicationDbContext(options2);
        var updatedUser = await context2.Users.FindAsync(userId);
        
        Assert.NotNull(updatedUser);
        Assert.Equal(appKey, updatedUser.KisAppKey);
        Assert.Equal(appSecret, updatedUser.KisAppSecret);
        Assert.Equal(accountNumber, updatedUser.AccountNumber);
    }

    [Fact]
    public async Task UpdateUserKisInfo_MultipleConcurrentUpdates_ShouldUpdateUser()
    {
        string dbName = "ConcurrentUpdatesTestDb";
        
        var options1 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context1 = new ApplicationDbContext(options1);
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
        
        // 두 번째 컨텍스트 생성 (다른 클라이언트 시뮬레이션)
        var options2 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context2 = new ApplicationDbContext(options2);
        var logger2 = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger<KisTokenRepository>();
        var repository2 = new UserKisInfoRepository(context2, logger2);
        
        // 두 리포지토리에서 순차적으로 업데이트 수행
        await repository1.UpdateUserKisInfo(userId, "app_key_1", "app_secret_1", "account_1");
        await repository2.UpdateUserKisInfo(userId, "app_key_2", "app_secret_2", "account_2");
        
        // 새 컨텍스트로 확인
        var options3 = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        
        using var context3 = new ApplicationDbContext(options3);
        var finalUser = await context3.Users.FindAsync(userId);
        
        Assert.NotNull(finalUser);
        
        // 마지막 값으로 업데이트되어 있어야 함
        Assert.Equal("app_key_2", finalUser.KisAppKey);
        Assert.Equal("app_secret_2", finalUser.KisAppSecret);
        Assert.Equal("account_2", finalUser.AccountNumber);
    }
}