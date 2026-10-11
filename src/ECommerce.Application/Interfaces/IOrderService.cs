using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Orders;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(CheckoutDto dto, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderDto>> GetMyOrdersAsync(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<OrderDto> GetMyOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PagedResult<OrderDto>> GetAdminOrdersAsync(OrderFilterParams filter, CancellationToken cancellationToken = default);
    Task<OrderDto> GetAdminOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto dto, CancellationToken cancellationToken = default);
}
