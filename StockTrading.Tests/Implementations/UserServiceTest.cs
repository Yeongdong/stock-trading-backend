using Google.Apis.Auth;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.DataAccess.Repositories;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Implementations;

[TestSubject(typeof(UserService))]
public class UserServiceTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockDbTransactionWrapper;
    private readonly UserService _userService;

    public UserServiceTest()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockDbTransactionWrapper = new Mock<IDbTransactionWrapper>();

        _mockDbTransactionWrapper
            .Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);

        _mockDbTransactionWrapper
            .Setup(t => t.RollbackAsync())
            .Returns(Task.CompletedTask);

        _mockDbTransactionWrapper
            .Setup(t => t.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        _mockDbContextWrapper
            .Setup(x => x.BeginTransactionAsync())
            .ReturnsAsync(_mockDbTransactionWrapper.Object);

        _userService = new UserService(
            _mockUserRepository.Object,
            _mockLogger.Object,
            _mockDbContextWrapper.Object
        );
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_ExistingUser_ReturnsUserDto()
    {
        var googleId = "google123";
        var existingUser = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = googleId,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = googleId,
            Email = "test@example.com",
            Name = "Test User"
        };

        _mockUserRepository.Setup(repo => repo.GetByGoogleIdAsync(googleId))
            .ReturnsAsync(existingUser);

        var result = await _userService.GetOrCreateGoogleUserAsync(payload);

        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result.Id);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.Name, result.Name);

        // 검증: 사용자 추가가 호출되지 않음
        _mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
    }
    
    [Fact]
    public async Task GetOrCreateGoogleUserAsync_NewUser_CreatesUserAndReturnsUserDto()
    {
        var googleId = "new_google_user";
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = googleId,
            Email = "newuser@example.com",
            Name = "New User"
        };
        
        var newUser = new User
        {
            Id = 42,
            Email = payload.Email,
            Name = payload.Name,
            GoogleId = payload.Subject,
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };

        _mockUserRepository.Setup(repo => repo.GetByGoogleIdAsync(googleId))
            .ReturnsAsync((User)null);

        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(newUser);

        var result = await _userService.GetOrCreateGoogleUserAsync(payload);

        Assert.NotNull(result);
        Assert.Equal(payload.Email, result.Email);
        Assert.Equal(payload.Name, result.Name);

        _mockUserRepository.Verify(repo => repo.AddAsync(It.Is<User>(
            u => u.Email == payload.Email && u.GoogleId == payload.Subject)), Times.Once);
    }
    
    [Fact]
    public async Task GetOrCreateGoogleUserAsync_NullPayload_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _userService.GetOrCreateGoogleUserAsync(null));
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_AddUserFails_RollsBackTransaction()
    {
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "new_google_user",
            Email = "failuser@example.com",
            Name = "Fail User"
        };

        _mockUserRepository.Setup(repo => repo.GetByGoogleIdAsync(payload.Subject))
            .ReturnsAsync((User)null);

        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ThrowsAsync(new Exception("DB Error"));

        await Assert.ThrowsAsync<Exception>(() =>
            _userService.GetOrCreateGoogleUserAsync(payload));

        _mockDbTransactionWrapper.Verify(t => t.RollbackAsync(), Times.Once);
        _mockDbTransactionWrapper.Verify(t => t.CommitAsync(), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateGoogleUserAsync_NewUser_CommitsTransaction()
    {
        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "commit_test_google",
            Email = "commit@example.com",
            Name = "Commit User"
        };

        var newUser = new User
        {
            Id = 99,
            Email = payload.Email,
            Name = payload.Name,
            GoogleId = payload.Subject,
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };

        _mockUserRepository.Setup(repo => repo.GetByGoogleIdAsync(payload.Subject))
            .ReturnsAsync((User)null);

        _mockUserRepository.Setup(repo => repo.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(newUser);

        var result = await _userService.GetOrCreateGoogleUserAsync(payload);

        _mockDbTransactionWrapper.Verify(t => t.CommitAsync(), Times.Once);
        _mockDbTransactionWrapper.Verify(t => t.RollbackAsync(), Times.Never);
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingEmail_ReturnsUserDto()
    {
        // Arrange
        var email = "test@example.com";
        var existingUser = new User
        {
            Id = 1,
            Email = email,
            Name = "Test User",
            Role = "User"
        };

        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(existingUser);

        var result = await _userService.GetUserByEmailAsync(email);

        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result.Id);
        Assert.Equal(existingUser.Email, result.Email);
        Assert.Equal(existingUser.Name, result.Name);
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistingEmail_ThrowsKeyNotFoundException()
    {
        var email = "nonexistent@example.com";

        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync((User)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _userService.GetUserByEmailAsync(email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetUserByEmailAsync_InvalidEmail_ThrowsArgumentException(string email)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _userService.GetUserByEmailAsync(email));
    }
    
    [Fact]
    public async Task GetUserByEmailAsync_UserWithoutKisToken_ReturnsDtoWithNullKisToken()
    {
        var email = "nokistoken@example.com";
        var user = new User
        {
            Id = 1,
            Email = email,
            Name = "No Token User",
            Role = "User",
            KisToken = null
        };

        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(email))
            .ReturnsAsync(user);

        var result = await _userService.GetUserByEmailAsync(email);

        Assert.Null(result.KisToken);
    }

    [Fact]
    public async Task GetUserByEmailAsync_RepositoryException_PropagatesException()
    {
        var email = "error@example.com";

        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(email))
            .ThrowsAsync(new Exception("Repository error"));

        await Assert.ThrowsAsync<Exception>(() =>
            _userService.GetUserByEmailAsync(email));
    }
}