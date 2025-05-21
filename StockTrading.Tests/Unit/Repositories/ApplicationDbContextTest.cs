using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Infrastructure.Security.Encryption;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Unit.Repositories;

[TestSubject(typeof(ApplicationDbContext))]
public class ApplicationDbContextTest
{
    private readonly Mock<IEncryptionService> _mockEncryptionService;

    public ApplicationDbContextTest()
    {
        _mockEncryptionService = new Mock<IEncryptionService>();

        _mockEncryptionService.Setup(s => s.Encrypt(It.IsAny<string>()))
            .Returns<string>(input => input == null ? null : $"encrypted_{input}");

        _mockEncryptionService.Setup(s => s.Decrypt(It.IsAny<string>()))
            .Returns<string>(input => input == null ? null :
                input.StartsWith("encrypted_") ? input.Substring("encrypted_".Length) : input);
    }

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, _mockEncryptionService.Object);
    }

    /*
     * User 엔티티의 테이블 이름, 컬럼 이름, 인덱스 설정이 올바른지 검증
     */
    [Fact]
    public void UserEntity_HasCorrectConfiguration()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(User));

        Assert.NotNull(entityType);
        Assert.Equal("users", entityType.GetTableName());
        Assert.Equal("id", entityType.FindProperty(nameof(User.Id)).GetColumnName());
        Assert.Equal("email", entityType.FindProperty(nameof(User.Email)).GetColumnName());
        Assert.Equal("name", entityType.FindProperty(nameof(User.Name)).GetColumnName());
        Assert.Equal("google_id", entityType.FindProperty(nameof(User.GoogleId)).GetColumnName());
        Assert.Equal("created_at", entityType.FindProperty(nameof(User.CreatedAt)).GetColumnName());
        Assert.Equal("role", entityType.FindProperty(nameof(User.Role)).GetColumnName());
        Assert.Equal("password_hash", entityType.FindProperty(nameof(User.PasswordHash)).GetColumnName());
        Assert.Equal("kis_app_key", entityType.FindProperty(nameof(User.KisAppKey)).GetColumnName());
        Assert.Equal("kis_app_secret", entityType.FindProperty(nameof(User.KisAppSecret)).GetColumnName());
        Assert.Equal("account_number", entityType.FindProperty(nameof(User.AccountNumber)).GetColumnName());

        // 인덱스 확인
        var emailIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(User.Email)));
        Assert.NotNull(emailIndex);
        Assert.True(emailIndex.IsUnique);
        Assert.Equal("ix_users_email", emailIndex.GetDatabaseName());

        var googleIdIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(User.GoogleId)));
        Assert.NotNull(googleIdIndex);
        Assert.Equal("ix_users_google_id", googleIdIndex.GetDatabaseName());
    }

    /*
     * KisToken 엔티티의 테이블 이름, 컬럼 이름 검증
     * User와의 관계 설정(외래 키, 삭제 동작)이 올바르게 구성되었는지 확인
     */
    [Fact]
    public void KisTokenEntity_HasCorrectConfiguration()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(KisToken));

        Assert.NotNull(entityType);
        Assert.Equal("kis_tokens", entityType.GetTableName());
        Assert.Equal("access_token", entityType.FindProperty(nameof(KisToken.AccessToken)).GetColumnName());
        Assert.Equal("expires_in", entityType.FindProperty(nameof(KisToken.ExpiresIn)).GetColumnName());
        Assert.Equal("token_type", entityType.FindProperty(nameof(KisToken.TokenType)).GetColumnName());

        var foreignKey = entityType.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        Assert.NotNull(foreignKey);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    /*
     * 실제 User 엔티티 추가 및 조회 기능 테스트
     * 저장 후 조회한 데이터가 올바른지 확인
     */
    [Fact]
    public async Task CanAddAndRetrieveUser()
    {
        await using var context = CreateContext();
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

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("Test User", savedUser.Name);
        Assert.Equal("google123", savedUser.GoogleId);
    }

    /*
     * User와 연관된 KisToken을 함께 저장/조회 테스트
     * Include를 사용한 관계 로딩 검증
     */
    [Fact]
    public async Task CanAddAndRetrieveUserWithKisToken()
    {
        await using var context = CreateContext();
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

        var savedUser = await context.Users
            .Include(u => u.KisToken)
            .FirstOrDefaultAsync(u => u.Email == "test@example.com");

        Assert.NotNull(savedUser);
        Assert.NotNull(savedUser.KisToken);
        Assert.Equal("test_access_token", savedUser.KisToken.AccessToken);
        Assert.Equal("Bearer", savedUser.KisToken.TokenType);
    }

    /*
     * 삭제 동작(Cascade) 테스트
     * User 삭제 시 연관된 KisToken도 함께 삭제되는지 확인
     */
    [Fact]
    public async Task DeleteUser_CascadesKisToken()
    {
        await using var context = CreateContext();
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

        var userId = user.Id;

        context.Users.Remove(user);
        await context.SaveChangesAsync();

        var kisToken = await context.KisTokens.FirstOrDefaultAsync(t => t.UserId == userId);
        Assert.Null(kisToken);
    }
}