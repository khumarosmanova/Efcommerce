using AppValidationException = ECommerce.Application.Common.Exceptions.ValidationException;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[Authorize]
[Route("api/[controller]")]
public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IValidator<CheckoutDto> _checkoutValidator;

    public OrdersController(
        IOrderService orderService,
        IValidator<CheckoutDto> checkoutValidator)
    {
        _orderService = orderService;
        _checkoutValidator = checkoutValidator;
    }

    /// <summary>
    /// Creates an order from current user's shopping cart within a database transaction.
    /// Validates stock, decrements quantities, snapshots item names/prices, and clears the cart.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _checkoutValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _orderService.CheckoutAsync(dto, cancellationToken);
        return Success(result, "Order placed successfully.");
    }

    /// <summary>
    /// Retrieves paginated order history for the currently authenticated user.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetMyOrdersAsync(page, pageSize, cancellationToken);
        return Success(result, "My orders retrieved successfully.");
    }

    /// <summary>
    /// Retrieves full order details for a specific order owned by current user.
    /// </summary>
    [HttpGet("my/{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrderById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetMyOrderByIdAsync(id, cancellationToken);
        return Success(result, "Order retrieved successfully.");
    }
}
