using Google.Apis.Auth;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.DataAccess.Repositories;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Implementations;

[TestSubject(typeof(UserService))]
public class UserServiceTest
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserService _userService;

    public UserServiceTest()
    {
        // 인메모리 데이터베이스 설정
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(_options);

        // 의존성 모킹 설정
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();

        // 서비스 인스턴스 생성
        _userService = new UserService(
            _mockUserRepository.Object,
            _mockLogger.Object,
            _dbContext
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
    public async Task GetUserByEmailAsync_RepositoryException_PropagatesException()
    {
        var email = "error@example.com";

        _mockUserRepository.Setup(repo => repo.GetByEmailAsync(email))
            .ThrowsAsync(new Exception("Repository error"));

        await Assert.ThrowsAsync<Exception>(() =>
            _userService.GetUserByEmailAsync(email));
    }
}