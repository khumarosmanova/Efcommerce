using ECommerce.Application.DTOs.Coupons;

namespace ECommerce.Application.Interfaces;

public interface ICouponService
{
    Task<IReadOnlyList<CouponDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CouponDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CouponDto> CreateAsync(CreateCouponDto dto, CancellationToken cancellationToken = default);
    Task<CouponDto> UpdateAsync(int id, UpdateCouponDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<CouponValidationResultDto> ValidateCouponAsync(string code, decimal cartTotal, CancellationToken cancellationToken = default);
}
