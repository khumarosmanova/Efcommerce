using AppValidationException = ECommerce.Application.Common.Exceptions.ValidationException;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[Route("api/admin/users")]
[HasPermission(Permissions.User.Manage)]
public class AdminUsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IValidator<UpdateUserRolesDto> _updateUserRolesValidator;

    public AdminUsersController(
        IUserService userService,
        IValidator<UpdateUserRolesDto> updateUserRolesValidator)
    {
        _userService = userService;
        _updateUserRolesValidator = updateUserRolesValidator;
    }

    /// <summary>
    /// Retrieves a paginated list of users with search and role filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserManagementDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] UserFilterParams filter,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(filter, cancellationToken);
        return Success(result, "Users retrieved successfully.");
    }

    /// <summary>
    /// Retrieves full user details including assigned roles, permissions, and lockout status.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return Success(result, "User retrieved successfully.");
    }

    /// <summary>
    /// Assigns or replaces roles for a given user.
    /// Prevents a user from removing their own SuperAdmin role or leaving the system without a SuperAdmin.
    /// Updates security stamp and revokes active refresh tokens to force fresh claims.
    /// </summary>
    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserRoles(
        Guid id,
        [FromBody] UpdateUserRolesDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateUserRolesValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _userService.UpdateUserRolesAsync(id, dto, cancellationToken);
        return Success(result, "User roles updated successfully.");
    }

    /// <summary>
    /// Locks or unlocks a user account.
    /// Prevents users from locking their own account or locking the last active SuperAdmin.
    /// When locked, updates security stamp and revokes active refresh tokens immediately.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid id,
        [FromBody] UpdateUserStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserStatusAsync(id, dto, cancellationToken);
        var message = dto.IsLocked ? "User has been locked." : "User has been unlocked.";
        return Success(result, message);
    }
}
