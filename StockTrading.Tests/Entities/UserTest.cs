using FluentAssertions;
using JetBrains.Annotations;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Entities;

[TestSubject(typeof(User))]
public class UserTest
{

    [Fact]
    public void User_Properties_ShouldSetAndGetCorrectly()
    {
        var id = 1;
        var email = "test@example.com";
        var name = "Test User";
        var googleId = "google123456";
        var createdAt = DateTime.UtcNow;
        var role = "User";
        var passwordHash = "hashedPassword123";
        var kisAppKey = "appKey123";
        var kisAppSecret = "appSecret456";
        var accountNumber = "12345678";
        var webSocketToken = "wsToken987";

        var user = new User
        {
            Id = id,
            Email = email,
            Name = name,
            GoogleId = googleId,
            CreatedAt = createdAt,
            Role = role,
            PasswordHash = passwordHash,
            KisAppKey = kisAppKey,
            KisAppSecret = kisAppSecret,
            AccountNumber = accountNumber,
            WebSocketToken = webSocketToken
        };

        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.GoogleId.Should().Be(googleId);
        user.CreatedAt.Should().Be(createdAt);
        user.Role.Should().Be(role);
        user.PasswordHash.Should().Be(passwordHash);
        user.KisAppKey.Should().Be(kisAppKey);
        user.KisAppSecret.Should().Be(kisAppSecret);
        user.AccountNumber.Should().Be(accountNumber);
        user.WebSocketToken.Should().Be(webSocketToken);
    }
    
    [Fact]
    public void User_NullableProperties_CanBeNull()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User",
        };

        user.PasswordHash.Should().BeNull();
        user.KisAppKey.Should().BeNull();
        user.KisAppSecret.Should().BeNull();
        user.AccountNumber.Should().BeNull();
        user.KisToken.Should().BeNull();
        user.WebSocketToken.Should().BeNull();
    }

    [Fact]
    public void User_RequiredProperties_ShouldNotBeNull()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };

        user.Email.Should().NotBeNull();
        user.Name.Should().NotBeNull();
        user.GoogleId.Should().NotBeNull();
        user.Role.Should().NotBeNull();
    }
    
    [Fact]
    public void User_KisTokenRelationship_ShouldSetAndGetCorrectly()
    {
        var userId = 42;
        var kisToken = new KisToken
        {
            Id = 1,
            AccessToken = "testAccessToken",
            ExpiresIn = DateTime.UtcNow.AddHours(2),
            TokenType = "Bearer",
            UserId = userId
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User",
            KisToken = kisToken
        };

        user.KisToken.Should().NotBeNull();
        user.KisToken.Should().BeSameAs(kisToken);
        user.KisToken.UserId.Should().Be(user.Id);
    }
    
    [Fact]
    public void User_WithKisApi_ShouldHaveRequiredValues()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User",
            KisAppKey = "appKey123",
            KisAppSecret = "appSecret456",
            AccountNumber = "12345678"
        };

        user.KisAppKey.Should().NotBeNull();
        user.KisAppSecret.Should().NotBeNull();
        user.AccountNumber.Should().NotBeNull();
            
        var canUseKisApi = !string.IsNullOrEmpty(user.KisAppKey) &&
                           !string.IsNullOrEmpty(user.KisAppSecret) &&
                           !string.IsNullOrEmpty(user.AccountNumber);
            
        canUseKisApi.Should().BeTrue();
    }
    
    [Fact]
    public void User_WithoutKisApi_ShouldNotBeAbleToUseKisApi()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };

        var canUseKisApi = !string.IsNullOrEmpty(user.KisAppKey) &&
                           !string.IsNullOrEmpty(user.KisAppSecret) &&
                           !string.IsNullOrEmpty(user.AccountNumber);

        canUseKisApi.Should().BeFalse();
    }
    
    [Fact]
    public void User_DefaultCreatedAt_ShouldNotBeDefault()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User"
        };

        user.CreatedAt.Should().NotBe(default(DateTime));
    }
    
    [Fact]
    public void User_WithOAuth_ShouldNotRequirePassword()
    {
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            GoogleId = "google123",
            CreatedAt = DateTime.UtcNow,
            Role = "User",
            PasswordHash = null // 비밀번호 없음
        };

        var isOAuthUser = !string.IsNullOrEmpty(user.GoogleId);
        var requiresPassword = !isOAuthUser && string.IsNullOrEmpty(user.PasswordHash);

        isOAuthUser.Should().BeTrue();
        requiresPassword.Should().BeFalse();
    }
}