using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WishlistController : BaseApiController
{
    private readonly IWishlistService _wishlistService;
    private readonly ICurrentUserService _currentUserService;

    public WishlistController(
        IWishlistService wishlistService,
        ICurrentUserService currentUserService)
    {
        _wishlistService = wishlistService;
        _currentUserService = currentUserService;
    }

    private bool IsAdminUser() =>
        _currentUserService.Roles.Any(r => r is "SuperAdmin" or "Admin" or "Moderator");

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(_currentUserService.UserId, out userId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWishlist(CancellationToken cancellationToken)
    {
        if (IsAdminUser())
        {
            return Success(new List<ProductDto>(), "Admins have no wishlist items.");
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        var result = await _wishlistService.GetWishlistAsync(userId, cancellationToken);
        return Success(result, "Wishlist retrieved successfully.");
    }

    [HttpGet("ids")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWishlistIds(CancellationToken cancellationToken)
    {
        if (IsAdminUser())
        {
            return Success(new List<int>(), "Admins have no wishlist items.");
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        var result = await _wishlistService.GetWishlistProductIdsAsync(userId, cancellationToken);
        return Success(result, "Wishlist IDs retrieved successfully.");
    }

    [HttpPost("{productId:int}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Toggle(int productId, CancellationToken cancellationToken)
    {
        if (IsAdminUser())
        {
            return BadRequest(ApiResponse.Failure("Administrator accounts cannot add products to wishlist."));
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        var isAdded = await _wishlistService.ToggleWishlistAsync(userId, productId, cancellationToken);
        var message = isAdded ? "Product added to wishlist." : "Product removed from wishlist.";
        return Success(new { isAdded, productId }, message);
    }

    [HttpPost("{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Add(int productId, CancellationToken cancellationToken)
    {
        if (IsAdminUser())
        {
            return BadRequest(ApiResponse.Failure("Administrator accounts cannot add products to wishlist."));
        }

        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        await _wishlistService.AddToWishlistAsync(userId, productId, cancellationToken);
        return Success("Product added to wishlist.");
    }

    [HttpDelete("{productId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Remove(int productId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(ApiResponse.Failure("Invalid user token."));
        }

        await _wishlistService.RemoveFromWishlistAsync(userId, productId, cancellationToken);
        return Success("Product removed from wishlist.");
    }
}
