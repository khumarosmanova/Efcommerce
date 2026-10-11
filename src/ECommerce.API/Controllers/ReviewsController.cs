using System.Security.Claims;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Reviews;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
public class ReviewsController : BaseApiController
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUserService;

    public ReviewsController(
        IReviewService reviewService,
        ICurrentUserService currentUserService)
    {
        _reviewService = reviewService;
        _currentUserService = currentUserService;
    }

    [HttpGet("~/api/products/{productId:int}/reviews")]
    [HttpGet("~/api/reviews/product/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductRatingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductReviews(int productId, CancellationToken cancellationToken)
    {
        var result = await _reviewService.GetProductReviewsAsync(productId, cancellationToken);
        return Success(result, "Product reviews retrieved successfully.");
    }

    [Authorize]
    [HttpPost("~/api/products/{productId:int}/reviews")]
    [HttpPost("~/api/reviews/product/{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddReview(int productId, [FromBody] CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var userIdStr = _currentUserService.UserId;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        var userName = _currentUserService.UserName ?? "Customer";
        var userEmail = _currentUserService.Email ?? "";

        var result = await _reviewService.AddReviewAsync(productId, dto, userId, userName, userEmail, cancellationToken);
        return Success(result, "Review posted successfully.");
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteReview(int id, CancellationToken cancellationToken)
    {
        var userIdStr = _currentUserService.UserId;
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        var isAdmin = User.IsInRole("SuperAdmin") || User.IsInRole("Admin");
        await _reviewService.DeleteReviewAsync(id, userId, isAdmin, cancellationToken);

        return Success("Review deleted successfully.");
    }
}
