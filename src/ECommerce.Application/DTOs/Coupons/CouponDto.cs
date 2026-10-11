using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs.Coupons;

public class CouponDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CouponDiscountType DiscountType { get; set; }
    public string DiscountTypeDisplay => DiscountType == CouponDiscountType.Percentage ? "Percentage" : "Fixed";
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;
    public bool IsLimitReached => UsageLimit.HasValue && TimesUsed >= UsageLimit.Value;
    public bool IsValid => IsActive && !IsExpired && !IsLimitReached;
    public DateTime CreatedAt { get; set; }
}
