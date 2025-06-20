using JetBrains.Annotations;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Exceptions.Authentication;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;

namespace StockTrading.Tests.Unit.Helpers;

[TestSubject(typeof(KisValidationHelper))]
public class KisValidationHelperTest
{
    [Fact]
    public void ValidateUserForKisApi_ValidUser_NoException()
    {
        // Arrange
        var validUser = CreateValidUser();

        // Act & Assert
        var exception = Record.Exception(() => KisValidationHelper.ValidateUserForKisApi(validUser));
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateUserForKisApi_NullUser_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            KisValidationHelper.ValidateUserForKisApi(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTokenRequest_InvalidAppSecret_ThrowsArgumentException(string appSecret)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateTokenRequest(1, "appKey", appSecret, "accountNumber"));

        Assert.Equal("KIS 앱 시크릿이 설정되지 않았습니다. (Parameter 'appSecret')", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTokenRequest_InvalidAccountNumber_ThrowsArgumentException(string accountNumber)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateTokenRequest(1, "appKey", "appSecret", accountNumber));

        Assert.Equal("계좌번호가 설정되지 않았습니다. (Parameter 'accountNumber')", exception.Message);
    }

    private static UserInfo CreateValidUser()
    {
        return new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "validAppKey",
            KisAppSecret = "validAppSecret",
            AccountNumber = "1234567890",
            KisToken = new KisTokenInfo
            {
                Id = 1,
                AccessToken = "validAccessToken",
                TokenType = "Bearer",
                ExpiresIn = DateTime.UtcNow.AddHours(1) // 1시간 후 만료
            }
        };
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserForKisApi_InvalidKisAppKey_ThrowsArgumentException(string kisAppKey)
    {
        // Arrange
        var user = CreateValidUser();
        user.KisAppKey = kisAppKey;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateUserForKisApi(user));

        Assert.Equal("KIS 앱 키가 설정되지 않았습니다. (Parameter 'appKey')", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserForKisApi_InvalidKisAppSecret_ThrowsArgumentException(string kisAppSecret)
    {
        // Arrange
        var user = CreateValidUser();
        user.KisAppSecret = kisAppSecret;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateUserForKisApi(user));

        Assert.Equal("KIS 앱 시크릿이 설정되지 않았습니다. (Parameter 'appSecret')", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUserForKisApi_InvalidAccountNumber_ThrowsArgumentException(string accountNumber)
    {
        // Arrange
        var user = CreateValidUser();
        user.AccountNumber = accountNumber;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateUserForKisApi(user));

        Assert.Equal("계좌번호가 설정되지 않았습니다. (Parameter 'accountNumber')", exception.Message);
    }

    [Fact]
    public void ValidateUserForKisApi_NullKisToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateValidUser();
        user.KisToken = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            KisValidationHelper.ValidateUserForKisApi(user));

        Assert.Equal("KIS 액세스 토큰이 없습니다. 토큰을 먼저 발급받아주세요.", exception.Message);
    }

    [Fact]
    public void ValidateUserForKisApi_NullAccessToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateValidUser();
        user.KisToken.AccessToken = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            KisValidationHelper.ValidateUserForKisApi(user));

        Assert.Equal("KIS 액세스 토큰이 없습니다. 토큰을 먼저 발급받아주세요.", exception.Message);
    }

    [Fact]
    public void ValidateUserForKisApi_ExpiredToken_ThrowsKisTokenExpiredException()
    {
        // Arrange
        var user = CreateValidUser();
        user.KisToken.ExpiresIn = DateTime.UtcNow.AddMinutes(-1); // 1분 전 만료

        // Act & Assert
        var exception = Assert.Throws<KisTokenExpiredException>(() =>
            KisValidationHelper.ValidateUserForKisApi(user));

        Assert.Equal("KIS 액세스 토큰이 만료되었습니다.", exception.Message);
    }

    [Fact]
    public void ValidateTokenRequest_ValidParameters_NoException()
    {
        // Act & Assert - 예외가 발생하지 않아야 함
        var exception = Record.Exception(() =>
            KisValidationHelper.ValidateTokenRequest(1, "appKey", "appSecret", "accountNumber"));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateTokenRequest_InvalidUserId_ThrowsArgumentException(int userId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateTokenRequest(userId, "appKey", "appSecret", "accountNumber"));

        Assert.Equal("유효하지 않은 사용자 ID입니다. (Parameter 'userId')", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTokenRequest_InvalidAppKey_ThrowsArgumentException(string appKey)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            KisValidationHelper.ValidateTokenRequest(1, appKey, "appSecret", "accountNumber"));

        Assert.Equal("KIS 앱 키가 설정되지 않았습니다. (Parameter 'appKey')", exception.Message);
    }
}