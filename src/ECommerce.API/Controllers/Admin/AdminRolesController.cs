using AppValidationException = ECommerce.Application.Common.Exceptions.ValidationException;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Roles;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Admin;

[Route("api/admin/roles")]
[HasPermission(Permissions.Role.Manage)]
public class AdminRolesController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly IValidator<CreateRoleDto> _createRoleValidator;
    private readonly IValidator<UpdateRoleDto> _updateRoleValidator;

    public AdminRolesController(
        IRoleService roleService,
        IValidator<CreateRoleDto> createRoleValidator,
        IValidator<UpdateRoleDto> updateRoleValidator)
    {
        _roleService = roleService;
        _createRoleValidator = createRoleValidator;
        _updateRoleValidator = updateRoleValidator;
    }

    /// <summary>
    /// Returns all available system permissions grouped by module name.
    /// </summary>
    [HttpGet("/api/admin/permissions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ModulePermissionsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPermissionsGroupedByModule(CancellationToken cancellationToken)
    {
        var result = await _roleService.GetPermissionsGroupedByModuleAsync(cancellationToken);
        return Success(result, "Permissions grouped by module retrieved successfully.");
    }

    /// <summary>
    /// Retrieves all roles with their assigned permissions and assigned user count.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await _roleService.GetAllRolesAsync(cancellationToken);
        return Success(result, "Roles retrieved successfully.");
    }

    /// <summary>
    /// Retrieves role details and its permissions by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetRoleByIdAsync(id, cancellationToken);
        return Success(result, "Role retrieved successfully.");
    }

    /// <summary>
    /// Creates a new role and assigns permission claims.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateRoleDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createRoleValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _roleService.CreateRoleAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<RoleDto>.Success(result, "Role created successfully."));
    }

    /// <summary>
    /// Updates a role's name, description, and synchronizes its permission claims.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateRoleValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _roleService.UpdateRoleAsync(id, dto, cancellationToken);
        return Success(result, "Role updated successfully.");
    }

    /// <summary>
    /// Deletes a role. Forbids deleting SuperAdmin or roles that still have users.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteRoleAsync(id, cancellationToken);
        return Success("Role deleted successfully.");
    }
}
