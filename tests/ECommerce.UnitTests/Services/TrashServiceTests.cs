using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.UnitTests.Services;

public class TrashServiceTests
{
    [Fact]
    public async Task RestoreProduct_When_Category_Is_Deleted_Throws_ValidationException()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category
        {
            Name = "Hardware",
            Slug = "hardware",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Monitor",
            Slug = "monitor",
            SKU = "MON-001",
            Price = 250m,
            StockQuantity = 5,
            CategoryId = category.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var act = async () => await trashService.RestoreProductAsync(product.Id);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*restore the category first*");
    }

    [Fact]
    public async Task RestoreProduct_When_Category_Is_Active_Succeeds()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category
        {
            Name = "Hardware",
            Slug = "hardware",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Keyboard",
            Slug = "keyboard",
            SKU = "KB-001",
            Price = 50m,
            StockQuantity = 15,
            CategoryId = category.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "some-user",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var result = await trashService.RestoreProductAsync(product.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Keyboard");
        result.IsCategoryDeleted.Should().BeFalse();

        var rawProduct = await context.Products.IgnoreQueryFilters().FirstAsync(p => p.Id == product.Id);
        rawProduct.IsDeleted.Should().BeFalse();
        rawProduct.DeletedAt.Should().BeNull();
        rawProduct.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task RestoreCategory_When_Parent_Is_Deleted_Throws_ValidationException()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var parentCategory = new Category
        {
            Name = "Parent Dept",
            Slug = "parent-dept",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(parentCategory);
        await context.SaveChangesAsync();

        var subCategory = new Category
        {
            Name = "Child Dept",
            Slug = "child-dept",
            ParentCategoryId = parentCategory.Id,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(subCategory);
        await context.SaveChangesAsync();

        // Act
        var act = async () => await trashService.RestoreCategoryAsync(subCategory.Id);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*restore the parent category first*");
    }

    [Fact]
    public async Task RestoreCategory_When_Active_Succeeds()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category
        {
            Name = "Appliances",
            Slug = "appliances",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "admin",
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        // Act
        var result = await trashService.RestoreCategoryAsync(category.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Appliances");

        var rawCategory = await context.Categories.IgnoreQueryFilters().FirstAsync(c => c.Id == category.Id);
        rawCategory.IsDeleted.Should().BeFalse();
        rawCategory.DeletedAt.Should().BeNull();
        rawCategory.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task HardDeleteProduct_When_Referenced_By_OrderItem_Throws_ValidationException()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category { Name = "Fashion", Slug = "fashion", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Jacket",
            Slug = "jacket",
            SKU = "JK-001",
            Price = 120m,
            StockQuantity = 10,
            CategoryId = category.Id,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        var order = new Order
        {
            OrderNumber = "ORD-TEST-1",
            UserId = "user-1",
            Status = OrderStatus.Delivered,
            TotalAmount = 120m,
            ShippingAddress = "123 Main St",
            CreatedAt = DateTime.UtcNow
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = 1,
            CreatedAt = DateTime.UtcNow
        };
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        // Act
        var act = async () => await trashService.HardDeleteProductAsync(product.Id);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*referenced in past customer orders*");
    }

    [Fact]
    public async Task HardDeleteProduct_When_Not_Referenced_By_OrderItem_Physically_Deletes()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category { Name = "Toys", Slug = "toys", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Puzzle",
            Slug = "puzzle",
            SKU = "PZ-001",
            Price = 20m,
            StockQuantity = 50,
            CategoryId = category.Id,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        var cart = new Cart { UserId = "user-cart", CreatedAt = DateTime.UtcNow };
        await context.Carts.AddAsync(cart);
        await context.SaveChangesAsync();

        var cartItem = new CartItem { CartId = cart.Id, ProductId = product.Id, Quantity = 2 };
        await context.CartItems.AddAsync(cartItem);
        await context.SaveChangesAsync();

        // Act
        await trashService.HardDeleteProductAsync(product.Id);

        // Assert
        var rawProduct = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.Id);
        rawProduct.Should().BeNull();

        var remainingCartItems = await context.CartItems.Where(ci => ci.ProductId == product.Id).ToListAsync();
        remainingCartItems.Should().BeEmpty();
    }

    [Fact]
    public async Task HardDeleteCategory_When_Referenced_By_Products_Throws_ValidationException()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category
        {
            Name = "Sports",
            Slug = "sports",
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Football",
            Slug = "football",
            SKU = "FB-001",
            Price = 30m,
            StockQuantity = 20,
            CategoryId = category.Id,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var act = async () => await trashService.HardDeleteCategoryAsync(category.Id);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*product(s) still reference it*");
    }

    [Fact]
    public async Task HardDeleteCategory_When_Referenced_By_Subcategories_Throws_ValidationException()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var parent = new Category
        {
            Name = "Vehicles",
            Slug = "vehicles",
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(parent);
        await context.SaveChangesAsync();

        var child = new Category
        {
            Name = "Bicycles",
            Slug = "bicycles",
            ParentCategoryId = parent.Id,
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(child);
        await context.SaveChangesAsync();

        // Act
        var act = async () => await trashService.HardDeleteCategoryAsync(parent.Id);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*subcategory(ies) reference it as parent*");
    }

    [Fact]
    public async Task HardDeleteCategory_When_Empty_Physically_Deletes()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();
        var trashService = new TrashService(unitOfWork);

        var category = new Category
        {
            Name = "Empty Dept",
            Slug = "empty-dept",
            IsDeleted = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        // Act
        await trashService.HardDeleteCategoryAsync(category.Id);

        // Assert
        var rawCategory = await context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == category.Id);
        rawCategory.Should().BeNull();
    }
}
