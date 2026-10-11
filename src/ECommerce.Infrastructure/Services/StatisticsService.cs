using ECommerce.Application.DTOs.Statistics;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _context;

    public StatisticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StatisticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalRevenue = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount, cancellationToken);

        var totalOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var customerRoleId = await _context.Roles
            .AsNoTracking()
            .Where(r => r.Name == "Customer")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var totalCustomers = customerRoleId != Guid.Empty
            ? await _context.UserRoles.CountAsync(ur => ur.RoleId == customerRoleId, cancellationToken)
            : await _context.Users.CountAsync(cancellationToken);

        // Active, non-deleted products (global query filter automatically excludes soft-deleted)
        var totalProducts = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.IsActive, cancellationToken);

        var pendingOrders = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);

        // Low stock count: products with stock <= 5
        var lowStockProductCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.StockQuantity <= 5, cancellationToken);

        return new StatisticsSummaryDto
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            PendingOrders = pendingOrders,
            LowStockProductCount = lowStockProductCount
        };
    }

    public async Task<IReadOnlyList<SalesTimeSeriesDto>> GetSalesTimeSeriesAsync(
        SalesQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var isMonthly = string.Equals(parameters.GroupBy, "month", StringComparison.OrdinalIgnoreCase);

        var toDate = parameters.To ?? DateTime.UtcNow;
        var toUtc = toDate.Date.AddDays(1).AddTicks(-1);

        var fromDate = parameters.From ?? (isMonthly ? toDate.AddMonths(-11) : toDate.AddDays(-29));
        var fromUtc = fromDate.Date;

        var ordersQuery = _context.Orders
            .AsNoTracking()
            .Where(o => o.Status != OrderStatus.Cancelled &&
                        o.CreatedAt >= fromUtc &&
                        o.CreatedAt <= toUtc);

        if (isMonthly)
        {
            var rawSales = await ordersQuery
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .ToListAsync(cancellationToken);

            var salesDict = rawSales.ToDictionary(
                s => $"{s.Year:D4}-{s.Month:D2}",
                s => (s.Revenue, s.OrderCount));

            // Generate full continuous monthly sequence
            var result = new List<SalesTimeSeriesDto>();
            var cursor = new DateTime(fromUtc.Year, fromUtc.Month, 1);
            var endMonth = new DateTime(toUtc.Year, toUtc.Month, 1);

            while (cursor <= endMonth)
            {
                var periodKey = cursor.ToString("yyyy-MM");
                salesDict.TryGetValue(periodKey, out var tuple);

                result.Add(new SalesTimeSeriesDto
                {
                    Period = periodKey,
                    Date = cursor,
                    Revenue = tuple.Revenue,
                    OrderCount = tuple.OrderCount
                });

                cursor = cursor.AddMonths(1);
            }

            return result;
        }
        else
        {
            var rawSales = await ordersQuery
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month, o.CreatedAt.Day })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .ToListAsync(cancellationToken);

            var salesDict = rawSales.ToDictionary(
                s => $"{s.Year:D4}-{s.Month:D2}-{s.Day:D2}",
                s => (s.Revenue, s.OrderCount));

            // Generate full continuous daily sequence
            var result = new List<SalesTimeSeriesDto>();
            var cursor = fromUtc.Date;
            var endDate = toUtc.Date;

            while (cursor <= endDate)
            {
                var periodKey = cursor.ToString("yyyy-MM-dd");
                salesDict.TryGetValue(periodKey, out var tuple);

                result.Add(new SalesTimeSeriesDto
                {
                    Period = periodKey,
                    Date = cursor,
                    Revenue = tuple.Revenue,
                    OrderCount = tuple.OrderCount
                });

                cursor = cursor.AddDays(1);
            }

            return result;
        }
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopSellingProductsAsync(
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(top <= 0 ? 10 : top, 1, 100);

        var topItems = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order != null && oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                SnapshotName = g.Select(oi => oi.ProductName).FirstOrDefault()
            })
            .OrderByDescending(g => g.TotalQuantitySold)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (!topItems.Any())
        {
            return Array.Empty<TopProductDto>();
        }

        var productIds = topItems.Select(t => t.ProductId).ToList();

        // Use IgnoreQueryFilters so even soft-deleted products retain their SKU and image info
        var products = await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return topItems.Select(t =>
        {
            products.TryGetValue(t.ProductId, out var prod);
            return new TopProductDto
            {
                ProductId = t.ProductId,
                ProductName = prod?.Name ?? t.SnapshotName ?? $"Product #{t.ProductId}",
                SKU = prod?.SKU,
                ImageUrl = prod?.ImageUrl,
                TotalQuantitySold = t.TotalQuantitySold,
                TotalRevenue = t.TotalRevenue
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<CategorySalesDto>> GetCategorySalesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Fetch order items for non-cancelled orders
        var orderItems = await _context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order != null && oi.Order.Status != OrderStatus.Cancelled)
            .Select(oi => new
            {
                oi.ProductId,
                oi.Quantity,
                Revenue = oi.Quantity * oi.UnitPrice
            })
            .ToListAsync(cancellationToken);

        if (!orderItems.Any())
        {
            return Array.Empty<CategorySalesDto>();
        }

        var productIds = orderItems.Select(oi => oi.ProductId).Distinct().ToList();

        // 2. Map ProductId -> CategoryId using IgnoreQueryFilters (works even if product is soft-deleted)
        var products = await _context.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.CategoryId })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var categoryIds = products.Values.Select(p => p.CategoryId).Distinct().ToList();

        // 3. Map CategoryId -> CategoryName using IgnoreQueryFilters (works even if category is soft-deleted)
        var categories = await _context.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        // 4. Group by category in memory
        var categoryGroups = orderItems
            .GroupBy(oi => products.TryGetValue(oi.ProductId, out var p) ? p.CategoryId : 0)
            .Select(g =>
            {
                var catId = g.Key;
                var catName = catId != 0 && categories.TryGetValue(catId, out var c)
                    ? c.Name
                    : (catId != 0 ? $"Category #{catId}" : "Uncategorized");

                return new CategorySalesDto
                {
                    CategoryId = catId,
                    CategoryName = catName,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Revenue)
                };
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToList();

        var totalAllRevenue = categoryGroups.Sum(c => c.TotalRevenue);
        foreach (var stat in categoryGroups)
        {
            stat.RevenuePercentage = totalAllRevenue > 0
                ? Math.Round((double)(stat.TotalRevenue / totalAllRevenue) * 100, 2)
                : 0;
        }

        return categoryGroups;
    }

    public async Task<IReadOnlyList<OrderStatusStatisticsDto>> GetOrdersByStatusAsync(CancellationToken cancellationToken = default)
    {
        var orderGroups = await _context.Orders
            .AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var totalOrdersCount = orderGroups.Sum(g => g.Count);
        var allStatuses = Enum.GetValues<OrderStatus>();

        return allStatuses.Select(status =>
        {
            var group = orderGroups.FirstOrDefault(g => g.Status == status);
            var count = group?.Count ?? 0;
            var totalAmount = group?.TotalAmount ?? 0m;
            var percentage = totalOrdersCount > 0
                ? Math.Round(((double)count / totalOrdersCount) * 100, 2)
                : 0;

            return new OrderStatusStatisticsDto
            {
                Status = status.ToString(),
                OrderCount = count,
                TotalAmount = totalAmount,
                Percentage = percentage
            };
        }).ToList();
    }
}
