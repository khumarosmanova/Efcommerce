namespace ECommerce.Application.DTOs.Orders;

public class CheckoutDto
{
    public string ShippingAddress { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}
