using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Repositories;

[TestSubject(typeof(UserRepository))]
public class UserRepositoryTest
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_ReturnsUser_WhenUserExists()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            KisToken = new KisToken
            {
                AccessToken = "test_access_token",
                ExpiresIn = DateTime.UtcNow.AddMinutes(5),
                TokenType = "Bearer"
            }
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.GetByGoogleIdAsync("google123");

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);
        
        var result = await repository.GetByGoogleIdAsync("nonexistent");
        
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddUser_AndReturnsUserWithId()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        var newUser = new User
        {
            Email = "new@example.com",
            Name = "New User",
            GoogleId = "google456",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            KisToken = new KisToken
            {
                AccessToken = "test_access_token",
                ExpiresIn = DateTime.UtcNow.AddMinutes(5),
                TokenType = "Bearer"
            }
        };
        
        var result = await repository.AddAsync(newUser);
        
        Assert.NotEqual(0, result.Id);
        var savedUser = await context.Users.FindAsync(result.Id);
        Assert.NotNull(savedUser);
        Assert.Equal("new@example.com", savedUser.Email);
        Assert.Equal("New User", savedUser.Name);
        Assert.Equal("google456", savedUser.GoogleId);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenUserExists()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

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

        var result = await repository.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
    }
    
    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        var result = await repository.GetByIdAsync(999); // 존재하지 않는 ID

        Assert.Null(result);
    }
    
    [Fact]
    public async Task GetByEmailAsync_ReturnsUserDto_WhenUserExists()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            AccountNumber = "123456789",
            KisAppKey = "app-key-123",
            KisAppSecret = "app-secret-456"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmailAsync("test@example.com");

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("123456789", result.AccountNumber);
        Assert.Equal("app-key-123", result.KisAppKey);
        Assert.Equal("app-secret-456", result.KisAppSecret);
        Assert.Null(result.KisToken); // KisToken이 null인지 확인
    }
    
    [Fact]
    public async Task GetByEmailAsync_ReturnsUserDtoWithKisToken_WhenUserHasKisToken()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            AccountNumber = "123456789",
            KisAppKey = "app-key-123",
            KisAppSecret = "app-secret-456"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
            
        var kisToken = new KisToken
        {
            UserId = user.Id,
            AccessToken = "access-token-123",
            ExpiresIn = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer"
        };
        context.KisTokens.Add(kisToken);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmailAsync("test@example.com");

        Assert.NotNull(result);
        Assert.NotNull(result.KisToken);
        Assert.Equal("access-token-123", result.KisToken.AccessToken);
        Assert.Equal("Bearer", result.KisToken.TokenType);
    }
    
    [Fact]
    public async Task GetByEmailAsync_ThrowsException_WhenUserDoesNotExist()
    {
        using var context = CreateContext();
        var repository = new UserRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.GetByEmailAsync("nonexistent@example.com"));
    }
}