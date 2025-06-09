using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Security.Encryption;
using StockTrading.Infrastructure.Services;
using StockTrading.Infrastructure.Services.Common;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(DbContextWrapper))]
public class DbContextWrapperTest
{

    [Fact]
    public async Task BeginTransactionAsync_ShouldReturnDbTransactionWrapper()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        var mockEncryptionService = new Mock<IEncryptionService>();
            
        var mockDbContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>(), mockEncryptionService.Object);
        var mockDatabase = new Mock<DatabaseFacade>(mockDbContext.Object);
            
        mockDbContext.Setup(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);
            
        var dbContextWrapper = new DbContextWrapper(mockDbContext.Object);

        var result = await dbContextWrapper.BeginTransactionAsync();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IDbTransactionWrapper>(result);
    
        // 기능 테스트
        await result.CommitAsync(); // 예외가 발생하지 않으면 OK
        await result.DisposeAsync(); // 예외가 발생하지 않으면 OK
    }
}