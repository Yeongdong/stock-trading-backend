using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using StockTrading.Infrastructure.Services;
using StockTrading.Infrastructure.Services.Common;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(DbTransactionWrapper))]
public class DbTransactionWrapperTest
{
    [Fact]
    public async Task CommitAsync_ShouldCallTransactionCommit()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var transactionWrapper = new DbTransactionWrapper(mockTransaction.Object);

        await transactionWrapper.CommitAsync();

        mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_ShouldCallTransactionRollback()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var transactionWrapper = new DbTransactionWrapper(mockTransaction.Object);

        await transactionWrapper.RollbackAsync();

        mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCallTransactionDispose()
    {
        var mockTransaction = new Mock<IDbContextTransaction>();
        mockTransaction.Setup(t => t.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var transactionWrapper = new DbTransactionWrapper(mockTransaction.Object);

        await transactionWrapper.DisposeAsync();

        mockTransaction.Verify(t => t.DisposeAsync(), Times.Once);
    }
}