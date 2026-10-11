using ECommerce.Application.Common.Interfaces;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ECommerce.UnitTests.Common;

public static class TestDbContextFactory
{
    public static (AppDbContext Context, UnitOfWork UnitOfWork, Mock<ICurrentUserService> CurrentUserMock) CreateInMemoryContext(string? dbName = null)
    {
        var databaseName = dbName ?? Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.UserId).Returns("test-admin-guid");
        currentUserMock.Setup(u => u.UserName).Returns("admin@shop.com");
        currentUserMock.Setup(u => u.Email).Returns("admin@shop.com");
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var context = new AppDbContext(options, currentUserMock.Object);
        var unitOfWork = new UnitOfWork(context);

        return (context, unitOfWork, currentUserMock);
    }
}
