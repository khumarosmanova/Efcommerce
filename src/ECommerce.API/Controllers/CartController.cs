using AppValidationException = ECommerce.Application.Common.Exceptions.ValidationException;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CartController : BaseApiController
{
    private readonly ICartService _cartService;
    private readonly IValidator<AddToCartDto> _addValidator;
    private readonly IValidator<UpdateCartItemDto> _updateValidator;

    public CartController(
        ICartService cartService,
        IValidator<AddToCartDto> addValidator,
        IValidator<UpdateCartItemDto> updateValidator)
    {
        _cartService = cartService;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Retrieves current user's shopping cart.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var result = await _cartService.GetCartAsync(cancellationToken);
        return Success(result, "Cart retrieved successfully.");
    }

    /// <summary>
    /// Adds a product to the cart with stock and availability verification.
    /// </summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _addValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _cartService.AddItemAsync(dto, cancellationToken);
        return Success(result, "Item added to cart successfully.");
    }

    /// <summary>
    /// Updates quantity of an existing item in the cart.
    /// </summary>
    [HttpPut("items/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateCartItemDto dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _cartService.UpdateItemAsync(id, dto, cancellationToken);
        return Success(result, "Cart item updated successfully.");
    }

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    [HttpDelete("items/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveItem(int id, CancellationToken cancellationToken)
    {
        var result = await _cartService.RemoveItemAsync(id, cancellationToken);
        return Success(result, "Item removed from cart successfully.");
    }

    /// <summary>
    /// Clears all items from current user's shopping cart.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await _cartService.ClearCartAsync(cancellationToken);
        return Success("Shopping cart cleared successfully.");
    }
}
