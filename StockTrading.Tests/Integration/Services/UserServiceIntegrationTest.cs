using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockTrading.DataAccess.Repositories;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Services;

public class UserServiceIntegrationTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockTransaction;
    private readonly ILogger<UserService> _logger;
    private readonly UserService _userService;

    public UserServiceIntegrationTest()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockTransaction = new Mock<IDbTransactionWrapper>();
        _logger = new NullLogger<UserService>();

        _mockDbContextWrapper
            .Setup(db => db.BeginTransactionAsync())
            .ReturnsAsync(_mockTransaction.Object);

        _userService = new UserService(
            _mockUserRepository.Object,
            _logger,
            _mockDbContextWrapper.Object
        );
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_ExistingUser_ReturnsUserDto()
    {
        var googleId = "google_id_123";
        var existingUser = new User
        {
            Id = 1,
            Email = "existing@example.com",
            Name = "Existing User",
            GoogleId = googleId,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = googleId,
            Email = "existing@example.com",
            Name = "Existing User"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByGoogleIdAsync(googleId))
            .ReturnsAsync(existingUser);

        var result = await _userService.GetOrCreateGoogleUserAsync(payload);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("existing@example.com", result.Email);
        Assert.Equal("Existing User", result.Name);

        _mockUserRepository.Verify(
            repo => repo.AddAsync(It.IsAny<User>()),
            Times.Never);
        _mockDbContextWrapper.Verify(
            db => db.BeginTransactionAsync(),
            Times.Never);
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_NewUser_CreatesAndReturnsUserDto()
    {
        var googleId = "new_google_id_456";
        var newUser = new User
        {
            Id = 2,
            Email = "new@example.com",
            Name = "New User",
            GoogleId = googleId,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = googleId,
            Email = "new@example.com",
            Name = "New User"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByGoogleIdAsync(googleId))
            .ReturnsAsync((User)null);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(newUser);

        _mockTransaction
            .Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);

        var result = await _userService.GetOrCreateGoogleUserAsync(payload);

        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("New User", result.Name);

        _mockDbContextWrapper.Verify(
            db => db.BeginTransactionAsync(),
            Times.Once);
        _mockUserRepository.Verify(
            repo => repo.AddAsync(It.Is<User>(u =>
                u.Email == payload.Email &&
                u.Name == payload.Name &&
                u.GoogleId == payload.Subject &&
                u.Role == "User")),
            Times.Once);
        _mockTransaction.Verify(
            t => t.CommitAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_NullPayload_ThrowsNullReferenceException()
    {
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _userService.GetOrCreateGoogleUserAsync(null));
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_RepositoryException_RollsBackTransaction()
    {
        var googleId = "error_google_id";
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = googleId,
            Email = "error@example.com",
            Name = "Error User"
        };

        _mockUserRepository
            .Setup(repo => repo.GetByGoogleIdAsync(googleId))
            .ReturnsAsync((User)null);

        _mockUserRepository
            .Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("데이터베이스 오류"));

        _mockTransaction
            .Setup(t => t.RollbackAsync())
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<Exception>(() =>
            _userService.GetOrCreateGoogleUserAsync(payload));

        _mockTransaction.Verify(
            t => t.RollbackAsync(),
            Times.Once);
        _mockTransaction.Verify(
            t => t.CommitAsync(),
            Times.Never);
    }
    
    [Fact]
    public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUserDto()
    {
        var email = "existing@example.com";
        var existingUser = new User
        {
            Id = 1,
            Email = email,
            Name = "Existing User",
            GoogleId = "google_id_123",
            Role = "User",
            CreatedAt = DateTime.UtcNow,
            KisAppKey = "kis_app_key",
            KisAppSecret = "kis_app_secret",
            AccountNumber = "123456789",
            KisToken = new KisToken
            {
                Id = 1,
                UserId = 1,
                AccessToken = "access_token",
                ExpiresIn = DateTime.UtcNow.AddHours(1),
                TokenType = "Bearer"
            }
        };

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(existingUser);

        var result = await _userService.GetUserByEmailAsync(email);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(email, result.Email);
        Assert.Equal("Existing User", result.Name);
        Assert.Equal("kis_app_key", result.KisAppKey);
        Assert.Equal("kis_app_secret", result.KisAppSecret);
        Assert.Equal("123456789", result.AccountNumber);
        Assert.NotNull(result.KisToken);
        Assert.Equal("access_token", result.KisToken.AccessToken);
        Assert.Equal("Bearer", result.KisToken.TokenType);
    }
    
    [Fact]
    public async Task GetUserByEmailAsync_NonExistentEmail_ThrowsKeyNotFoundException()
    {
        var nonExistentEmail = "nonexistent@example.com";

        _mockUserRepository
            .Setup(repo => repo.GetByEmailAsync(nonExistentEmail))
            .ThrowsAsync(new KeyNotFoundException($"User with email {nonExistentEmail} not found"));

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _userService.GetUserByEmailAsync(nonExistentEmail));

        Assert.Contains(nonExistentEmail, exception.Message);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserByEmailAsync_InvalidEmail_ThrowsArgumentException(string invalidEmail)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _userService.GetUserByEmailAsync(invalidEmail));
    }
}