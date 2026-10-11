using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Orders;

public class OrderFilterParams
{
    public OrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; } // OrderNumber or customer email/name
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
