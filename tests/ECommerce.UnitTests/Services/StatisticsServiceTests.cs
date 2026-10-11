using ECommerce.Application.DTOs.Statistics;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Services;
using ECommerce.UnitTests.Common;
using FluentAssertions;
using Xunit;

namespace ECommerce.UnitTests.Services;

public class StatisticsServiceTests
{
    [Fact]
    public async Task GetSummary_Returns_Accurate_Metrics_And_Excludes_Cancelled_Orders_From_Revenue()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var statsService = new StatisticsService(context);

        var category = new Category { Name = "Tech", Slug = "tech", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        // 3 products: 2 active (1 low stock, 1 normal stock), 1 soft-deleted
        var p1 = new Product
        {
            Name = "Mouse",
            Slug = "mouse",
            SKU = "MS-01",
            Price = 25m,
            StockQuantity = 3, // Low stock (<= 5)
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var p2 = new Product
        {
            Name = "Monitor",
            Slug = "monitor",
            SKU = "MN-01",
            Price = 300m,
            StockQuantity = 20, // Normal stock
            CategoryId = category.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var p3 = new Product
        {
            Name = "Old Cable",
            Slug = "old-cable",
            SKU = "CB-01",
            Price = 10m,
            StockQuantity = 2,
            CategoryId = category.Id,
            IsActive = true,
            IsDeleted = true, // Soft-deleted
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddRangeAsync(p1, p2, p3);

        // 3 orders: 1 Confirmed ($200), 1 Pending ($100), 1 Cancelled ($500)
        var o1 = new Order
        {
            OrderNumber = "ORD-001",
            UserId = "user-1",
            Status = OrderStatus.Confirmed,
            TotalAmount = 200m,
            CreatedAt = DateTime.UtcNow
        };
        var o2 = new Order
        {
            OrderNumber = "ORD-002",
            UserId = "user-2",
            Status = OrderStatus.Pending,
            TotalAmount = 100m,
            CreatedAt = DateTime.UtcNow
        };
        var o3 = new Order
        {
            OrderNumber = "ORD-003",
            UserId = "user-3",
            Status = OrderStatus.Cancelled,
            TotalAmount = 500m,
            CreatedAt = DateTime.UtcNow
        };
        await context.Orders.AddRangeAsync(o1, o2, o3);
        await context.SaveChangesAsync();

        // Act
        var summary = await statsService.GetSummaryAsync();

        // Assert
        summary.TotalRevenue.Should().Be(300m); // 200 + 100 (excludes cancelled 500)
        summary.TotalOrders.Should().Be(3);
        summary.TotalProducts.Should().Be(2); // Only active, non-deleted
        summary.PendingOrders.Should().Be(1);
        summary.LowStockProductCount.Should().Be(1); // p1 has stock 3
    }

    [Fact]
    public async Task GetSalesTimeSeries_Excludes_Cancelled_Orders()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var statsService = new StatisticsService(context);

        var today = DateTime.UtcNow.Date;

        var o1 = new Order
        {
            OrderNumber = "ORD-1",
            UserId = "user-1",
            Status = OrderStatus.Delivered,
            TotalAmount = 150m,
            CreatedAt = today.AddHours(2)
        };
        var o2 = new Order
        {
            OrderNumber = "ORD-2",
            UserId = "user-2",
            Status = OrderStatus.Cancelled, // Must be excluded
            TotalAmount = 900m,
            CreatedAt = today.AddHours(4)
        };
        await context.Orders.AddRangeAsync(o1, o2);
        await context.SaveChangesAsync();

        // Act
        var result = await statsService.GetSalesTimeSeriesAsync(new SalesQueryParameters
        {
            From = today.AddDays(-1),
            To = today,
            GroupBy = "day"
        });

        // Assert
        result.Should().NotBeNull();
        var todayItem = result.FirstOrDefault(r => r.Period == today.ToString("yyyy-MM-dd"));
        todayItem.Should().NotBeNull();
        todayItem!.Revenue.Should().Be(150m);
        todayItem.OrderCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTopSellingProducts_Ranks_Correctly_And_Ignores_Cancelled_Orders()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var statsService = new StatisticsService(context);

        var category = new Category { Name = "Goods", Slug = "goods", CreatedAt = DateTime.UtcNow };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var p1 = new Product { Name = "Headphones", Slug = "headphones", SKU = "HP-1", Price = 100m, CategoryId = category.Id, CreatedAt = DateTime.UtcNow };
        var p2 = new Product { Name = "Mousepad", Slug = "mousepad", SKU = "MP-1", Price = 20m, CategoryId = category.Id, CreatedAt = DateTime.UtcNow };
        await context.Products.AddRangeAsync(p1, p2);
        await context.SaveChangesAsync();

        // Confirmed order with 5 headphones and 20 mousepads
        var activeOrder = new Order { OrderNumber = "O-1", UserId = "u1", Status = OrderStatus.Confirmed, TotalAmount = 900m, CreatedAt = DateTime.UtcNow };
        await context.Orders.AddAsync(activeOrder);
        await context.SaveChangesAsync();

        var item1 = new OrderItem { OrderId = activeOrder.Id, ProductId = p1.Id, ProductName = p1.Name, UnitPrice = 100m, Quantity = 5, CreatedAt = DateTime.UtcNow };
        var item2 = new OrderItem { OrderId = activeOrder.Id, ProductId = p2.Id, ProductName = p2.Name, UnitPrice = 20m, Quantity = 20, CreatedAt = DateTime.UtcNow };
        await context.OrderItems.AddRangeAsync(item1, item2);

        // Cancelled order with 50 headphones (should be ignored)
        var cancelledOrder = new Order { OrderNumber = "O-2", UserId = "u2", Status = OrderStatus.Cancelled, TotalAmount = 5000m, CreatedAt = DateTime.UtcNow };
        await context.Orders.AddAsync(cancelledOrder);
        await context.SaveChangesAsync();

        var item3 = new OrderItem { OrderId = cancelledOrder.Id, ProductId = p1.Id, ProductName = p1.Name, UnitPrice = 100m, Quantity = 50, CreatedAt = DateTime.UtcNow };
        await context.OrderItems.AddAsync(item3);
        await context.SaveChangesAsync();

        // Act
        var top = await statsService.GetTopSellingProductsAsync(top: 10);

        // Assert
        top.Should().HaveCount(2);
        top[0].ProductName.Should().Be("Mousepad");
        top[0].TotalQuantitySold.Should().Be(20);
        top[0].TotalRevenue.Should().Be(400m);

        top[1].ProductName.Should().Be("Headphones");
        top[1].TotalQuantitySold.Should().Be(5);
        top[1].TotalRevenue.Should().Be(500m);
    }

    [Fact]
    public async Task GetCategorySales_Preserves_Reporting_Even_When_Product_Or_Category_Is_SoftDeleted()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var statsService = new StatisticsService(context);

        var deletedCategory = new Category
        {
            Name = "Vintage Electronics",
            Slug = "vintage-electronics",
            IsDeleted = true, // Soft-deleted category
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.Categories.AddAsync(deletedCategory);
        await context.SaveChangesAsync();

        var deletedProduct = new Product
        {
            Name = "Retro Walkman",
            Slug = "retro-walkman",
            SKU = "RETRO-01",
            Price = 150m,
            CategoryId = deletedCategory.Id,
            IsDeleted = true, // Soft-deleted product
            DeletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await context.Products.AddAsync(deletedProduct);
        await context.SaveChangesAsync();

        var order = new Order
        {
            OrderNumber = "ORD-HISTORIC-1",
            UserId = "user-retro",
            Status = OrderStatus.Delivered,
            TotalAmount = 300m,
            CreatedAt = DateTime.UtcNow
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = deletedProduct.Id,
            ProductName = deletedProduct.Name,
            UnitPrice = 150m,
            Quantity = 2,
            CreatedAt = DateTime.UtcNow
        };
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        // Act
        var result = await statsService.GetCategorySalesAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].CategoryName.Should().Be("Vintage Electronics");
        result[0].TotalQuantitySold.Should().Be(2);
        result[0].TotalRevenue.Should().Be(300m);
        result[0].RevenuePercentage.Should().Be(100.0);
    }

    [Fact]
    public async Task GetOrdersByStatus_Calculates_Percentages_Accurately()
    {
        // Arrange
        var (context, _, _) = TestDbContextFactory.CreateInMemoryContext();
        var statsService = new StatisticsService(context);

        // 4 orders: 2 Delivered ($400), 1 Confirmed ($100), 1 Pending ($100)
        await context.Orders.AddRangeAsync(
            new Order { OrderNumber = "O1", UserId = "u1", Status = OrderStatus.Delivered, TotalAmount = 200m, CreatedAt = DateTime.UtcNow },
            new Order { OrderNumber = "O2", UserId = "u1", Status = OrderStatus.Delivered, TotalAmount = 200m, CreatedAt = DateTime.UtcNow },
            new Order { OrderNumber = "O3", UserId = "u1", Status = OrderStatus.Confirmed, TotalAmount = 100m, CreatedAt = DateTime.UtcNow },
            new Order { OrderNumber = "O4", UserId = "u1", Status = OrderStatus.Pending, TotalAmount = 100m, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await statsService.GetOrdersByStatusAsync();

        // Assert
        result.Should().NotBeNull();
        var delivered = result.First(r => r.Status == "Delivered");
        delivered.OrderCount.Should().Be(2);
        delivered.TotalAmount.Should().Be(400m);
        delivered.Percentage.Should().Be(50.0);

        var confirmed = result.First(r => r.Status == "Confirmed");
        confirmed.OrderCount.Should().Be(1);
        confirmed.Percentage.Should().Be(25.0);
    }
}
