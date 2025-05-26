using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Persistence.Repositories;
using StockTrading.Infrastructure.Security.Encryption;

namespace StockTrading.Tests.Unit.Repositories;

[TestSubject(typeof(UserRepository))]
public class UserRepositoryTest
{
    private readonly Mock<IEncryptionService> _mockEncryptionService = new();

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _mockEncryptionService.Object);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_ReturnsUser_WhenUserExists()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

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
        Assert.NotNull(result.KisToken);
        Assert.Equal("test_access_token", result.KisToken.AccessToken);
    }

    [Fact]
    public async Task GetByGoogleIdAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

        var result = await repository.GetByGoogleIdAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsUser_WhenUserExists()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

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

        var result = await repository.GetByEmailAsync("test@example.com");

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Test User", result.Name);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

        var result = await repository.GetByEmailAsync("nonexistent@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailWithTokenAsync_ReturnsUserWithToken_WhenUserExists()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

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

        var kisToken = new KisToken
        {
            UserId = user.Id,
            AccessToken = "access-token-123",
            ExpiresIn = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer"
        };
        context.KisTokens.Add(kisToken);
        await context.SaveChangesAsync();

        var result = await repository.GetByEmailWithTokenAsync("test@example.com");

        Assert.NotNull(result);
        Assert.NotNull(result.KisToken);
        Assert.Equal("access-token-123", result.KisToken.AccessToken);
        Assert.Equal("Bearer", result.KisToken.TokenType);
    }

    [Fact]
    public async Task GetByEmailWithTokenAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

        var result = await repository.GetByEmailWithTokenAsync("nonexistent@example.com");

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_AddsUser_AndReturnsUserWithId()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

        var newUser = new User
        {
            Email = "new@example.com",
            Name = "New User",
            GoogleId = "google456",
            Role = "User",
            CreatedAt = DateTime.UtcNow
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
    public async Task UpdateAsync_UpdatesUser_AndReturnsUpdatedUser()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

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

        user.Name = "Updated User";
        user.KisAppKey = "new-app-key";
        user.AccountNumber = "123456789";

        var result = await repository.UpdateAsync(user);

        Assert.Equal("Updated User", result.Name);
        Assert.Equal("new-app-key", result.KisAppKey);
        Assert.Equal("123456789", result.AccountNumber);

        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.Equal("Updated User", updatedUser.Name);
        Assert.Equal("new-app-key", updatedUser.KisAppKey);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenUserExists()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

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

        var result = await repository.ExistsAsync(user.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var logger = Mock.Of<ILogger<UserRepository>>();
        var repository = new UserRepository(context, logger);

        var result = await repository.ExistsAsync(999);

        Assert.False(result);
    }
}