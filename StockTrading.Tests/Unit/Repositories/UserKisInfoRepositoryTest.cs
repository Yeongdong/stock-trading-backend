using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Persistence.Repositories;
using StockTrading.Infrastructure.Security.Encryption;

namespace StockTrading.Tests.Unit.Repositories;

[TestSubject(typeof(UserKisInfoRepository))]
public class UserKisInfoRepositoryTest
{
    private readonly ILogger<TokenRepository> _logger;
    private readonly Mock<IEncryptionService> _mockEncryptionService;

    public UserKisInfoRepositoryTest()
    {
        _logger = new NullLogger<TokenRepository>();
        _mockEncryptionService = new Mock<IEncryptionService>();
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new ApplicationDbContext(options, _mockEncryptionService.Object);
    }
    
    [Fact]
    public async Task UpdateUserKisInfo_UpdatesUserInfo_WhenUserExists()
    {
        using var context = CreateContext();
        var repository = new UserKisInfoRepository(context, _logger);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userId = user.Id;
        var appKey = "test-app-key";
        var appSecret = "test-app-secret";
        var accountNumber = "123456789";

        await repository.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber);

        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(appKey, updatedUser.KisAppKey);
        Assert.Equal(appSecret, updatedUser.KisAppSecret);
        Assert.Equal(accountNumber, updatedUser.AccountNumber);
    }
    
    [Fact]
    public async Task UpdateUserKisInfo_ThrowsKeyNotFoundException_WhenUserDoesNotExist()
    {
        using var context = CreateContext();
        var repository = new UserKisInfoRepository(context, _logger);

        var nonExistentUserId = 999;
        var appKey = "test-app-key";
        var appSecret = "test-app-secret";
        var accountNumber = "123456789";

        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            repository.UpdateUserKisInfoAsync(nonExistentUserId, appKey, appSecret, accountNumber));
    }
    
    [Fact]
    public async Task UpdateUserKisInfo_PreservesOtherUserData_WhenUpdating()
    {
        using var context = CreateContext();
        var repository = new UserKisInfoRepository(context, _logger);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userId = user.Id;
        var appKey = "test-app-key";
        var appSecret = "test-app-secret";
        var accountNumber = "123456789";

        await repository.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber);

        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
            
        // KIS 정보가 업데이트되었는지 확인
        Assert.Equal(appKey, updatedUser.KisAppKey);
        Assert.Equal(appSecret, updatedUser.KisAppSecret);
        Assert.Equal(accountNumber, updatedUser.AccountNumber);
            
        // 다른 사용자 데이터가 보존되었는지 확인
        Assert.Equal("test@example.com", updatedUser.Email);
        Assert.Equal("Test User", updatedUser.Name);
        Assert.Equal("google123", updatedUser.GoogleId);
        Assert.Equal("User", updatedUser.Role);
    }
    
    [Fact]
    public async Task SaveWebSocketTokenAsync_UpdatesWebSocketToken_WhenUserExists()
    {
        using var context = CreateContext();
        var repository = new UserKisInfoRepository(context, _logger);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userId = user.Id;
        var approvalKey = "websocket-approval-key-12345";

        await repository.SaveWebSocketTokenAsync(userId, approvalKey);

        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
        Assert.Equal(approvalKey, updatedUser.WebSocketToken);
    }
    
    [Fact]
    public async Task SaveWebSocketTokenAsync_ThrowsKeyNotFoundException_WhenUserDoesNotExist()
    {
        using var context = CreateContext();
        var repository = new UserKisInfoRepository(context, _logger);

        var nonExistentUserId = 999;
        var approvalKey = "websocket-approval-key-12345";

        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            repository.SaveWebSocketTokenAsync(nonExistentUserId, approvalKey));
    }
    
    [Fact]
    public async Task SaveWebSocketTokenAsync_PreservesOtherUserData_WhenUpdating()
    {
        using var context = CreateContext();
        var repository = new UserKisInfoRepository(context, _logger);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            KisAppKey = "existing-app-key",
            KisAppSecret = "existing-app-secret",
            AccountNumber = "987654321"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userId = user.Id;
        var approvalKey = "websocket-approval-key-12345";

        await repository.SaveWebSocketTokenAsync(userId, approvalKey);

        var updatedUser = await context.Users.FindAsync(userId);
        Assert.NotNull(updatedUser);
            
        // WebSocketToken이 업데이트되었는지 확인
        Assert.Equal(approvalKey, updatedUser.WebSocketToken);
            
        // 다른 KIS 정보가 보존되었는지 확인
        Assert.Equal("existing-app-key", updatedUser.KisAppKey);
        Assert.Equal("existing-app-secret", updatedUser.KisAppSecret);
        Assert.Equal("987654321", updatedUser.AccountNumber);
    }

    [Fact]
    public async Task UpdateUserKisInfo_HandlesExceptions_AndLogsErrors()
    {
        var mockContext = new TestDbContextWithSaveError(Guid.NewGuid().ToString());
        var repository = new UserKisInfoRepository(mockContext, _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            repository.UpdateUserKisInfoAsync(1, "key", "secret", "account"));
    }
    
    [Fact]
    public async Task SaveWebSocketTokenAsync_HandlesExceptions_AndLogsErrors()
    {
        var mockContext = new TestDbContextWithSaveError(Guid.NewGuid().ToString());
        var repository = new UserKisInfoRepository(mockContext, _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            repository.SaveWebSocketTokenAsync(1, "token"));
    }
    
    // 예외 테스트를 위한 ApplicationDbContext 상속 클래스
    private class TestDbContextWithSaveError : ApplicationDbContext
    {
        public TestDbContextWithSaveError(string dbName) : base(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName) 
                .Options, Mock.Of<IEncryptionService>())
        {
            Users.Add(new User 
            { 
                Id = 1, 
                Email = "test@example.com",
                Name = "Test User",
                GoogleId = "test123",
                Role = "User",
            });
            SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated database error");
        }
    }
}