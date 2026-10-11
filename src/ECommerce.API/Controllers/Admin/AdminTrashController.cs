using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Trash;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[Route("api/admin/trash")]
[HasPermission(Permissions.Trash.Manage)]
public class AdminTrashController : BaseApiController
{
    private readonly ITrashService _trashService;

    public AdminTrashController(ITrashService trashService)
    {
        _trashService = trashService;
    }

    /// <summary>
    /// Retrieves a paginated list of soft-deleted products with search and sorting by DeletedAt.
    /// </summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TrashProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDeletedProducts(
        [FromQuery] TrashFilterParams filter,
        CancellationToken cancellationToken)
    {
        var result = await _trashService.GetDeletedProductsAsync(filter, cancellationToken);
        return Success(result, "Deleted products retrieved successfully.");
    }

    /// <summary>
    /// Retrieves a paginated list of soft-deleted categories with search and sorting by DeletedAt.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TrashCategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDeletedCategories(
        [FromQuery] TrashFilterParams filter,
        CancellationToken cancellationToken)
    {
        var result = await _trashService.GetDeletedCategoriesAsync(filter, cancellationToken);
        return Success(result, "Deleted categories retrieved successfully.");
    }

    /// <summary>
    /// Restores a soft-deleted product.
    /// Blocked if the product's category is still soft-deleted.
    /// </summary>
    [HttpPost("products/{id:int}/restore")]
    [ProducesResponseType(typeof(ApiResponse<TrashProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _trashService.RestoreProductAsync(id, cancellationToken);
        return Success(result, $"Product '{result.Name}' restored successfully.");
    }

    /// <summary>
    /// Restores a soft-deleted category.
    /// Does not automatically restore its products; they remain soft-deleted until restored individually.
    /// </summary>
    [HttpPost("categories/{id:int}/restore")]
    [ProducesResponseType(typeof(ApiResponse<TrashCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RestoreCategory(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _trashService.RestoreCategoryAsync(id, cancellationToken);
        return Success(result, $"Category '{result.Name}' restored successfully.");
    }

    /// <summary>
    /// Permanently deletes a product from the database.
    /// Blocked if the product is referenced by any past OrderItem.
    /// </summary>
    [HttpDelete("products/{id:int}/hard-delete")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HardDeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        await _trashService.HardDeleteProductAsync(id, cancellationToken);
        return Success("Product permanently deleted.");
    }

    /// <summary>
    /// Permanently deletes a category from the database.
    /// Blocked if any products or subcategories still reference it.
    /// </summary>
    [HttpDelete("categories/{id:int}/hard-delete")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HardDeleteCategory(
        int id,
        CancellationToken cancellationToken)
    {
        await _trashService.HardDeleteCategoryAsync(id, cancellationToken);
        return Success("Category permanently deleted.");
    }
}
