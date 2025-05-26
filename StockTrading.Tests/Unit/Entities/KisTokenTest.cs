using FluentAssertions;
using JetBrains.Annotations;
using StockTrading.Domain.Entities;

namespace StockTrading.Tests.Unit.Entities;

[TestSubject(typeof(KisToken))]
public class KisTokenTest
{

    [Fact]
    public void KisToken_Properties_ShouldSetAndGetCorrectly()
    {
        var tokenId = 1;
        var accessToken = "exampleAccessToken123";
        var expiresIn = DateTime.UtcNow.AddHours(2);
        var tokenType = "Bearer";
        var userId = 42;

        var kisToken = new KisToken
        {
            Id = tokenId,
            AccessToken = accessToken,
            ExpiresIn = expiresIn,
            TokenType = tokenType,
            UserId = userId
        };

        kisToken.Id.Should().Be(tokenId);
        kisToken.AccessToken.Should().Be(accessToken);
        kisToken.ExpiresIn.Should().Be(expiresIn);
        kisToken.TokenType.Should().Be(tokenType);
        kisToken.UserId.Should().Be(userId);
    }
    
    [Fact]
    public void KisToken_UserRelationship_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var userId = 42;
    
        var user = new User
        {
            Id = 42,
            Email = "test@test.com",
            Name = "Test",
            GoogleId = "google_test",
            CreatedAt = DateTime.Now,
            Role = "User"
        };
    
        var kisToken = new KisToken
        {
            Id = 1,
            AccessToken = "testAccessToken",
            ExpiresIn = DateTime.UtcNow.AddHours(2),
            TokenType = "Bearer",
            UserId = userId,
            User = user
        };

        kisToken.User.Should().NotBeNull();
        kisToken.User.Should().BeSameAs(user);
        kisToken.UserId.Should().Be(user.Id);
    }
}