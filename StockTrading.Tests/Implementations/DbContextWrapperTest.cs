using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Repositories;

namespace StockTrading.Tests.Implementations;

[TestSubject(typeof(DbContextWrapper))]
public class DbContextWrapperTest
{

    [Fact]
    public async Task BeginTransactionAsync_ShouldReturnDbTransactionWrapper()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
            
        var mockDbContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
        var mockDatabase = new Mock<DatabaseFacade>(mockDbContext.Object);
            
        mockDbContext.Setup(c => c.Database).Returns(mockDatabase.Object);
        mockDatabase.Setup(d => d.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);
            
        var dbContextWrapper = new DbContextWrapper(mockDbContext.Object);

        var result = await dbContextWrapper.BeginTransactionAsync();

        Assert.NotNull(result);
        Assert.IsType<DbTransactionWrapper>(result);
    }
}