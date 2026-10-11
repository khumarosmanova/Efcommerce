using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Identity.Models;
using ECommerce.Infrastructure.Identity.Services;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ECommerce.UnitTests.Services;

public class AuthServiceTests
{
    private static Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<AppRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<AppRole>>();
        return new Mock<RoleManager<AppRole>>(store.Object, null!, null!, null!, null!);
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_User_Not_Found()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();
        var tokenServiceMock = new Mock<ITokenService>();
        var (context, _, currentUserMock) = TestDbContextFactory.CreateInMemoryContext();
        var jwtOptions = Options.Create(new JwtSettings());
        var emailServiceMock = new Mock<IEmailService>();

        userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var authService = new AuthService(
            userManagerMock.Object,
            roleManagerMock.Object,
            tokenServiceMock.Object,
            context,
            currentUserMock.Object,
            jwtOptions,
            emailServiceMock.Object
        );

        var request = new LoginRequestDto
        {
            Email = "nonexistent@shop.com",
            Password = "Password123!"
        };

        // Act & Assert
        var act = async () => await authService.LoginAsync(request);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_Should_Throw_UnauthorizedException_When_User_Is_Locked_Out()
    {
        // Arrange
        var userManagerMock = MockUserManager();
        var roleManagerMock = MockRoleManager();
        var tokenServiceMock = new Mock<ITokenService>();
        var (context, _, currentUserMock) = TestDbContextFactory.CreateInMemoryContext();
        var jwtOptions = Options.Create(new JwtSettings());
        var emailServiceMock = new Mock<IEmailService>();

        var lockedUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "locked@shop.com",
            UserName = "locked@shop.com",
            LockoutEnd = DateTimeOffset.UtcNow.AddDays(7)
        };

        userManagerMock.Setup(u => u.FindByEmailAsync("locked@shop.com"))
            .ReturnsAsync(lockedUser);
        userManagerMock.Setup(u => u.IsLockedOutAsync(lockedUser))
            .ReturnsAsync(true);

        var authService = new AuthService(
            userManagerMock.Object,
            roleManagerMock.Object,
            tokenServiceMock.Object,
            context,
            currentUserMock.Object,
            jwtOptions,
            emailServiceMock.Object
        );

        var request = new LoginRequestDto
        {
            Email = "locked@shop.com",
            Password = "Password123!"
        };

        // Act & Assert
        var act = async () => await authService.LoginAsync(request);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("This account has been locked. Please contact support.");
    }
}
