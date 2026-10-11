using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Coupons;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Route("api/[controller]")]
public class CouponsController : BaseApiController
{
    private readonly ICouponService _couponService;

    public CouponsController(ICouponService couponService)
    {
        _couponService = couponService;
    }

    /// <summary>
    /// Validates a coupon code against a shopping cart total and computes discount.
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ApiResponse<CouponValidationResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validate(
        [FromQuery] string code,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken)
    {
        var result = await _couponService.ValidateCouponAsync(code, amount, cancellationToken);
        return Success(result, result.Message);
    }
}
