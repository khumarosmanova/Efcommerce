using ECommerce.Application.DTOs.Statistics;

namespace ECommerce.Application.Interfaces;

public interface IStatisticsService
{
    Task<StatisticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesTimeSeriesDto>> GetSalesTimeSeriesAsync(SalesQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductDto>> GetTopSellingProductsAsync(int top = 10, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySalesDto>> GetCategorySalesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderStatusStatisticsDto>> GetOrdersByStatusAsync(CancellationToken cancellationToken = default);
}
