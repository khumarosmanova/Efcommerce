using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Statistics;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[Route("api/admin/statistics")]
[HasPermission(Permissions.Statistics.View)]
public class AdminStatisticsController : BaseApiController
{
    private readonly IStatisticsService _statisticsService;

    public AdminStatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Returns high-level metrics: total revenue, total orders, total customers,
    /// total products, pending orders, and low-stock product count.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<StatisticsSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetSummaryAsync(cancellationToken);
        return Success(result, "Statistics summary retrieved successfully.");
    }

    /// <summary>
    /// Returns revenue and order count time series grouped by day or month (excluding cancelled orders).
    /// </summary>
    [HttpGet("sales")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SalesTimeSeriesDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSalesTimeSeries(
        [FromQuery] SalesQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetSalesTimeSeriesAsync(parameters, cancellationToken);
        return Success(result, "Sales time series retrieved successfully.");
    }

    /// <summary>
    /// Returns the top-selling products by quantity and revenue (excluding cancelled orders).
    /// </summary>
    [HttpGet("top-products")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopProducts(
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticsService.GetTopSellingProductsAsync(top, cancellationToken);
        return Success(result, "Top selling products retrieved successfully.");
    }

    /// <summary>
    /// Returns sales and revenue breakdown by category.
    /// Preserves reporting accuracy even for soft-deleted products or categories using snapshot data.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategorySalesDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCategorySales(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetCategorySalesAsync(cancellationToken);
        return Success(result, "Category sales statistics retrieved successfully.");
    }

    /// <summary>
    /// Returns orders breakdown grouped by order status with counts, total amounts, and percentage share.
    /// </summary>
    [HttpGet("orders-by-status")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderStatusStatisticsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrdersByStatus(CancellationToken cancellationToken)
    {
        var result = await _statisticsService.GetOrdersByStatusAsync(cancellationToken);
        return Success(result, "Orders by status statistics retrieved successfully.");
    }
}
