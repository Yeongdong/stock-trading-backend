using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.DataAccess.DTOs;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Infrastructure.Security.Encryption;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Repositories;

public class KisTokenRepositoryIntegrationTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly KisTokenRepository _repository;
    private readonly ILogger<KisTokenRepository> _logger;
    private readonly Mock<IEncryptionService> _mockEncryptionService;

    public KisTokenRepositoryIntegrationTest()
    {
        _mockEncryptionService = new Mock<IEncryptionService>();
        _mockEncryptionService.Setup(s => s.Encrypt(It.IsAny<string>()))
            .Returns<string>(input => input);
        _mockEncryptionService.Setup(s => s.Decrypt(It.IsAny<string>()))
            .Returns<string>(input => input);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options, _mockEncryptionService.Object);

        _logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<KisTokenRepository>();

        _repository = new KisTokenRepository(_context, _logger);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SaveKisToken_WithNoExistingToken_ShouldCreateNewToken()
    {
        var user = new User
        {
            Email = "integration_test@example.com",
            Name = "Integration Test User",
            GoogleId = "google_integration_test",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var tokenResponse = new TokenResponse
        {
            AccessToken = "integration_test_access_token",
            ExpiresIn = 86400,
            TokenType = "Bearer"
        };

        await _repository.SaveKisToken(userId, tokenResponse);

        var savedToken = await _context.KisTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);

        Assert.NotNull(savedToken);
        Assert.Equal(tokenResponse.AccessToken, savedToken.AccessToken);
        Assert.Equal(tokenResponse.TokenType, savedToken.TokenType);

        var expectedExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        var timeDifference = Math.Abs((expectedExpiry - savedToken.ExpiresIn).TotalMinutes);
        Assert.True(timeDifference < 5, "토큰 만료 시간이 예상 범위 내에 있어야 합니다.");
    }

    [Fact]
    public async Task SaveKisToken_WithExistingToken_ShouldUpdateToken()
    {
        var user = new User
        {
            Email = "update_test@example.com",
            Name = "Update Test User",
            GoogleId = "google_update_test",
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;

        var existingToken = new KisToken
        {
            UserId = userId,
            AccessToken = "old_access_token",
            ExpiresIn = DateTime.UtcNow.AddHours(-1),
            TokenType = "Bearer"
        };

        _context.KisTokens.Add(existingToken);
        await _context.SaveChangesAsync();

        var newTokenResponse = new TokenResponse
        {
            AccessToken = "updated_access_token",
            ExpiresIn = 86400,
            TokenType = "Bearer"
        };

        await _repository.SaveKisToken(userId, newTokenResponse);

        var updatedToken = await _context.KisTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);

        Assert.NotNull(updatedToken);
        Assert.Equal(newTokenResponse.AccessToken, updatedToken.AccessToken);
        Assert.Equal(newTokenResponse.TokenType, updatedToken.TokenType);

        var expectedExpiry = DateTime.UtcNow.AddSeconds(newTokenResponse.ExpiresIn);
        var timeDifference = Math.Abs((expectedExpiry - updatedToken.ExpiresIn).TotalMinutes);
        Assert.True(timeDifference < 5, "토큰 만료 시간이 예상 범위 내에 있어야 합니다.");

        // 토큰이 하나만 존재하는지 확인 (중복 생성 방지)
        var tokenCount = await _context.KisTokens.CountAsync(t => t.UserId == userId);
        Assert.Equal(1, tokenCount);
    }

    [Fact]
    public async Task SaveKisToken_WithNonExistentUser_ShouldThrowKeyNotFoundException()
    {
        var nonExistentUserId = 999;
        var tokenResponse = new TokenResponse
        {
            AccessToken = "test_access_token",
            ExpiresIn = 86400,
            TokenType = "Bearer"
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.SaveKisToken(nonExistentUserId, tokenResponse));
    }

    [Fact]
    public async Task SaveKisToken_WithDatabaseError_ShouldHandleException()
    {
        // 실제 DB 오류를 시뮬레이션하기 위한 모의 컨텍스트
        var mockOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "error_db")
            .Options;

        // 특수한 테스트 컨텍스트 생성
        using var errorContext = new TestDbContextWithSaveError(mockOptions);
        var errorRepository = new KisTokenRepository(errorContext, _logger);

        var userId = 1; // 모의 사용자 ID (TestDbContextWithSaveError에 미리 생성됨)
        var tokenResponse = new TokenResponse
        {
            AccessToken = "error_test_token",
            ExpiresIn = 86400,
            TokenType = "Bearer"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            errorRepository.SaveKisToken(userId, tokenResponse));
    }

// 예외 테스트를 위한 특수 컨텍스트 클래스
    private class TestDbContextWithSaveError : ApplicationDbContext
    {
        public TestDbContextWithSaveError(DbContextOptions<ApplicationDbContext> options)
            : base(options, Mock.Of<IEncryptionService>())
        {
            Users.Add(new User
            {
                Id = 1,
                Email = "error_test@example.com",
                Name = "Error Test User",
                GoogleId = "google_error_test",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            });
            SaveChanges();
        }

        // SaveChangesAsync 메서드 오버라이드하여 예외 발생
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated database error for testing");
        }
    }
}