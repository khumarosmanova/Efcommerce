using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.UnitTests.Infrastructure;

public class SoftDeleteTests
{
    [Fact]
    public async Task SoftDelete_Product_Sets_IsDeleted_DeletedAt_And_DeletedBy()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var category = new Category { Name = "Tech", Slug = "tech", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Laptop",
            Slug = "laptop",
            SKU = "TECH-001",
            Price = 999.99m,
            StockQuantity = 10,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act - remove triggers soft delete interception
        context.Products.Remove(product);
        await context.SaveChangesAsync();

        // Assert - Query with filter returns nothing
        var visibleProducts = await context.Products.ToListAsync();
        visibleProducts.Should().BeEmpty();

        // Assert - Query with IgnoreQueryFilters returns soft-deleted product
        var deletedProduct = await context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        deletedProduct.Should().NotBeNull();
        deletedProduct!.IsDeleted.Should().BeTrue();
        deletedProduct.DeletedAt.Should().NotBeNull();
        deletedProduct.DeletedBy.Should().Be("test-admin-guid");
    }

    [Fact]
    public async Task SoftDelete_Category_Cascades_SoftDelete_To_Active_Products()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var category = new Category { Name = "Smartphones", Slug = "smartphones", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var p1 = new Product
        {
            Name = "Phone 1",
            Slug = "phone-1",
            SKU = "PH-001",
            Price = 500m,
            StockQuantity = 5,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var p2 = new Product
        {
            Name = "Phone 2",
            Slug = "phone-2",
            SKU = "PH-002",
            Price = 700m,
            StockQuantity = 3,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddRangeAsync(p1, p2);
        await context.SaveChangesAsync();

        // Act - Soft-delete category
        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        // Assert - Category and products are soft deleted
        var allCategories = await context.Categories.ToListAsync();
        allCategories.Should().BeEmpty();

        var visibleProducts = await context.Products.ToListAsync();
        visibleProducts.Should().BeEmpty();

        var deletedProducts = await context.Products
            .IgnoreQueryFilters()
            .Where(p => p.CategoryId == category.Id)
            .ToListAsync();

        deletedProducts.Should().HaveCount(2);
        deletedProducts.Should().OnlyContain(p => p.IsDeleted && p.DeletedAt != null);
    }

    [Fact]
    public async Task Restoring_Category_Does_Not_UnDelete_Its_Products()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var category = new Category { Name = "Gaming", Slug = "gaming", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Console",
            Slug = "console",
            SKU = "GM-001",
            Price = 400m,
            StockQuantity = 10,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Cascade soft-delete
        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        // Act - Restore ONLY the category
        category.IsDeleted = false;
        await context.SaveChangesAsync();

        // Assert - Category is visible again
        var visibleCategories = await context.Categories.ToListAsync();
        visibleCategories.Should().HaveCount(1);
        visibleCategories[0].IsDeleted.Should().BeFalse();

        // Assert - Product remains deleted until manually restored
        var visibleProducts = await context.Products.ToListAsync();
        visibleProducts.Should().BeEmpty();

        var rawProduct = await context.Products.IgnoreQueryFilters().FirstAsync(p => p.Id == product.Id);
        rawProduct.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task HardDelete_Bypasses_SoftDelete_Interception_And_Physically_Deletes()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var category = new Category { Name = "Books", Slug = "books", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Novel",
            Slug = "novel",
            SKU = "BK-001",
            Price = 15m,
            StockQuantity = 100,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act - Hard Delete
        context.HardDelete(product);
        await context.SaveChangesAsync();

        // Assert - Entity is physically deleted from database
        var rawProduct = await context.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        rawProduct.Should().BeNull();
    }
}
