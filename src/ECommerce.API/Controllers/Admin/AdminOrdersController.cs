using AppValidationException = ECommerce.Application.Common.Exceptions.ValidationException;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Orders;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[Route("api/admin/orders")]
[HasPermission(Permissions.Order.Manage)]
public class AdminOrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IValidator<UpdateOrderStatusDto> _statusValidator;

    public AdminOrdersController(
        IOrderService orderService,
        IValidator<UpdateOrderStatusDto> statusValidator)
    {
        _orderService = orderService;
        _statusValidator = statusValidator;
    }

    /// <summary>
    /// Admin endpoint to retrieve all customer orders with filtering, search, and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderFilterParams filter,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetAdminOrdersAsync(filter, cancellationToken);
        return Success(result, "Admin orders retrieved successfully.");
    }

    /// <summary>
    /// Admin endpoint to retrieve full details for any customer order.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetAdminOrderByIdAsync(id, cancellationToken);
        return Success(result, "Order retrieved successfully.");
    }

    /// <summary>
    /// Updates an order status following valid state transitions.
    /// Transitioning to Cancelled returns reserved stock back to products.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateOrderStatusDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _statusValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, dto, cancellationToken);
        return Success(result, "Order status updated successfully.");
    }
}
