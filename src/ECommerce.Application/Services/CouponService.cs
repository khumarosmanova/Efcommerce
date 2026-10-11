using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.DTOs.Coupons;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class CouponService : ICouponService
{
    private readonly IUnitOfWork _unitOfWork;

    public CouponService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CouponDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var coupons = await _unitOfWork.Repository<Coupon>()
            .Query()
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return coupons.Select(MapToDto).ToList();
    }

    public async Task<CouponDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var coupon = await _unitOfWork.Repository<Coupon>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (coupon == null)
        {
            throw new NotFoundException($"Coupon with ID {id} was not found.");
        }

        return MapToDto(coupon);
    }

    public async Task<CouponDto> CreateAsync(CreateCouponDto dto, CancellationToken cancellationToken = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();

        var exists = await _unitOfWork.Repository<Coupon>()
            .Query()
            .AnyAsync(c => c.Code == code, cancellationToken);

        if (exists)
        {
            throw new ValidationException($"A coupon with code '{code}' already exists.");
        }

        if (dto.DiscountValue <= 0)
        {
            throw new ValidationException("Discount value must be greater than zero.");
        }

        if (dto.DiscountType == CouponDiscountType.Percentage && dto.DiscountValue > 100)
        {
            throw new ValidationException("Percentage discount cannot exceed 100%.");
        }

        var coupon = new Coupon
        {
            Code = code,
            Description = dto.Description?.Trim(),
            DiscountType = dto.DiscountType,
            DiscountValue = dto.DiscountValue,
            MinOrderAmount = dto.MinOrderAmount,
            MaxDiscountAmount = dto.MaxDiscountAmount,
            StartDate = dto.StartDate,
            ExpiryDate = dto.ExpiryDate,
            UsageLimit = dto.UsageLimit,
            IsActive = dto.IsActive,
            TimesUsed = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Coupon>().AddAsync(coupon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(coupon);
    }

    public async Task<CouponDto> UpdateAsync(int id, UpdateCouponDto dto, CancellationToken cancellationToken = default)
    {
        var coupon = await _unitOfWork.Repository<Coupon>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (coupon == null)
        {
            throw new NotFoundException($"Coupon with ID {id} was not found.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();

        var exists = await _unitOfWork.Repository<Coupon>()
            .Query()
            .AnyAsync(c => c.Code == code && c.Id != id, cancellationToken);

        if (exists)
        {
            throw new ValidationException($"Another coupon with code '{code}' already exists.");
        }

        if (dto.DiscountValue <= 0)
        {
            throw new ValidationException("Discount value must be greater than zero.");
        }

        if (dto.DiscountType == CouponDiscountType.Percentage && dto.DiscountValue > 100)
        {
            throw new ValidationException("Percentage discount cannot exceed 100%.");
        }

        coupon.Code = code;
        coupon.Description = dto.Description?.Trim();
        coupon.DiscountType = dto.DiscountType;
        coupon.DiscountValue = dto.DiscountValue;
        coupon.MinOrderAmount = dto.MinOrderAmount;
        coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
        coupon.StartDate = dto.StartDate;
        coupon.ExpiryDate = dto.ExpiryDate;
        coupon.UsageLimit = dto.UsageLimit;
        coupon.IsActive = dto.IsActive;

        _unitOfWork.Repository<Coupon>().Update(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(coupon);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var coupon = await _unitOfWork.Repository<Coupon>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (coupon == null)
        {
            throw new NotFoundException($"Coupon with ID {id} was not found.");
        }

        _unitOfWork.Repository<Coupon>().Delete(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CouponValidationResultDto> ValidateCouponAsync(
        string code,
        decimal cartTotal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = "Please provide a valid coupon code.",
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        var normalizedCode = code.Trim().ToUpperInvariant();

        var coupon = await _unitOfWork.Repository<Coupon>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == normalizedCode, cancellationToken);

        if (coupon == null)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = $"Coupon '{normalizedCode}' was not found.",
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        if (!coupon.IsActive)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = $"Coupon '{normalizedCode}' is currently inactive.",
                Code = coupon.Code,
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        var utcNow = DateTime.UtcNow;

        if (coupon.StartDate.HasValue && coupon.StartDate.Value > utcNow)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = $"Coupon '{normalizedCode}' is not active yet (starts on {coupon.StartDate.Value:yyyy-MM-dd}).",
                Code = coupon.Code,
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        if (coupon.ExpiryDate.HasValue && coupon.ExpiryDate.Value < utcNow)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = $"Coupon '{normalizedCode}' has expired.",
                Code = coupon.Code,
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        if (coupon.UsageLimit.HasValue && coupon.TimesUsed >= coupon.UsageLimit.Value)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = $"Coupon '{normalizedCode}' has reached its maximum usage limit.",
                Code = coupon.Code,
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        if (coupon.MinOrderAmount.HasValue && cartTotal < coupon.MinOrderAmount.Value)
        {
            return new CouponValidationResultDto
            {
                IsValid = false,
                Message = $"Minimum order amount for coupon '{normalizedCode}' is ${coupon.MinOrderAmount.Value:F2}. Current subtotal: ${cartTotal:F2}.",
                Code = coupon.Code,
                OriginalAmount = cartTotal,
                FinalAmount = cartTotal
            };
        }

        // Calculate discount
        decimal discountAmount;
        if (coupon.DiscountType == CouponDiscountType.Percentage)
        {
            discountAmount = Math.Round((cartTotal * coupon.DiscountValue) / 100m, 2);
            if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
            {
                discountAmount = coupon.MaxDiscountAmount.Value;
            }
        }
        else
        {
            discountAmount = Math.Min(cartTotal, coupon.DiscountValue);
        }

        var finalAmount = Math.Max(0, cartTotal - discountAmount);

        return new CouponValidationResultDto
        {
            IsValid = true,
            Message = $"Coupon '{normalizedCode}' applied successfully!",
            Code = coupon.Code,
            DiscountType = coupon.DiscountType,
            DiscountValue = coupon.DiscountValue,
            DiscountAmount = discountAmount,
            OriginalAmount = cartTotal,
            FinalAmount = finalAmount
        };
    }

    private static CouponDto MapToDto(Coupon coupon)
    {
        return new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Description = coupon.Description,
            DiscountType = coupon.DiscountType,
            DiscountValue = coupon.DiscountValue,
            MinOrderAmount = coupon.MinOrderAmount,
            MaxDiscountAmount = coupon.MaxDiscountAmount,
            StartDate = coupon.StartDate,
            ExpiryDate = coupon.ExpiryDate,
            UsageLimit = coupon.UsageLimit,
            TimesUsed = coupon.TimesUsed,
            IsActive = coupon.IsActive,
            CreatedAt = coupon.CreatedAt
        };
    }
}
