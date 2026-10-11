using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Services;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.UnitTests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetFilteredAsync_Should_Exclude_SoftDeleted_Products()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();

        var category = new Category
        {
            Name = "Laptops",
            Slug = "laptops",
            CreatedAt = DateTime.UtcNow
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var activeProduct = new Product
        {
            Name = "MacBook Pro",
            Slug = "macbook-pro",
            SKU = "APL-MBP",
            Price = 1999m,
            StockQuantity = 10,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var deletedProduct = new Product
        {
            Name = "Old Laptop",
            Slug = "old-laptop",
            SKU = "OLD-01",
            Price = 500m,
            StockQuantity = 0,
            CategoryId = category.Id,
            IsActive = true,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.Products.AddRange(activeProduct, deletedProduct);
        await context.SaveChangesAsync();

        var service = new ProductService(unitOfWork);

        // Act
        var result = await service.GetFilteredAsync(new ProductFilterParams());

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(p => p.Name == "MacBook Pro");
        result.Items.Should().NotContain(p => p.Name == "Old Laptop");
    }

    [Fact]
    public async Task CreateAsync_Should_Generate_Unique_Slug_Automatically()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();

        var category = new Category
        {
            Name = "Audio",
            Slug = "audio",
            CreatedAt = DateTime.UtcNow
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var service = new ProductService(unitOfWork);
        var dto = new CreateProductDto
        {
            Name = "Sony Wireless Headphones",
            SKU = "SNY-WH-01",
            Price = 299m,
            StockQuantity = 15,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act
        var result = await service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Slug.Should().Be("sony-wireless-headphones");
        result.Price.Should().Be(299m);
        result.CategoryName.Should().Be("Audio");
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_Product_And_Set_Audit_Fields()
    {
        // Arrange
        var (context, unitOfWork, _) = TestDbContextFactory.CreateInMemoryContext();

        var category = new Category
        {
            Name = "Gear",
            Slug = "gear",
            CreatedAt = DateTime.UtcNow
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Camping Tent",
            Slug = "camping-tent",
            SKU = "CMP-01",
            Price = 150m,
            StockQuantity = 5,
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new ProductService(unitOfWork);

        // Act
        await service.DeleteAsync(product.Id);

        // Assert
        // Standard query filter should hide soft-deleted item
        var visibleProducts = await context.Products.ToListAsync();
        visibleProducts.Should().BeEmpty();

        // Direct query ignoring filters should see soft-deleted
        var rawProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .IgnoreQueryFilters(context.Products)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        rawProduct.Should().NotBeNull();
        rawProduct!.IsDeleted.Should().BeTrue();
        rawProduct.DeletedAt.Should().NotBeNull();
        rawProduct.DeletedBy.Should().Be("test-admin-guid");
    }
}
