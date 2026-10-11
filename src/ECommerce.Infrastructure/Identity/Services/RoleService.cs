using System.Security.Claims;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.DTOs.Roles;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Identity.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public RoleService(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        AppDbContext context)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var result = new List<RoleDto>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims
                .Where(c => c.Type == "permission" && !string.IsNullOrWhiteSpace(c.Value))
                .Select(c => c.Value)
                .OrderBy(p => p)
                .ToList();

            var userCount = await _context.UserRoles
                .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

            result.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                Description = role.Description,
                Permissions = permissions,
                UserCount = userCount
            });
        }

        return result;
    }

    public async Task<RoleDto> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            throw new NotFoundException($"Role with ID '{id}' was not found.");
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == "permission" && !string.IsNullOrWhiteSpace(c.Value))
            .Select(c => c.Value)
            .OrderBy(p => p)
            .ToList();

        var userCount = await _context.UserRoles
            .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            Permissions = permissions,
            UserCount = userCount
        };
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto, CancellationToken cancellationToken = default)
    {
        var roleName = dto.Name.Trim();
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            throw new ValidationException($"Role '{roleName}' already exists.");
        }

        var validPermissions = Permissions.GetAllPermissions();
        var invalidPermissions = dto.Permissions
            .Where(p => !validPermissions.Contains(p, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (invalidPermissions.Count > 0)
        {
            throw new ValidationException($"Invalid permissions: {string.Join(", ", invalidPermissions)}.");
        }

        var role = new AppRole(roleName, dto.Description?.Trim());
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => e.Description).ToList());
        }

        var distinctPermissions = dto.Permissions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var permission in distinctPermissions)
        {
            await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
        }

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            Permissions = distinctPermissions.OrderBy(p => p).ToList(),
            UserCount = 0
        };
    }

    public async Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleDto dto, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            throw new NotFoundException($"Role with ID '{id}' was not found.");
        }

        var newName = dto.Name.Trim();

        // Protect SuperAdmin role name
        if (string.Equals(role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The 'SuperAdmin' role name cannot be modified.");
        }

        if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            var existingWithSameName = await _roleManager.FindByNameAsync(newName);
            if (existingWithSameName != null && existingWithSameName.Id != role.Id)
            {
                throw new ValidationException($"Role '{newName}' already exists.");
            }

            role.Name = newName;
        }

        role.Description = dto.Description?.Trim();

        var updateResult = await _roleManager.UpdateAsync(role);
        if (!updateResult.Succeeded)
        {
            throw new ValidationException(updateResult.Errors.Select(e => e.Description).ToList());
        }

        var validPermissions = Permissions.GetAllPermissions();
        var invalidPermissions = dto.Permissions
            .Where(p => !validPermissions.Contains(p, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (invalidPermissions.Count > 0)
        {
            throw new ValidationException($"Invalid permissions: {string.Join(", ", invalidPermissions)}.");
        }

        // Sync permissions / RoleClaims
        var currentClaims = await _roleManager.GetClaimsAsync(role);
        var currentPermissionClaims = currentClaims.Where(c => c.Type == "permission").ToList();

        var targetPermissions = dto.Permissions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Remove removed claims
        foreach (var claim in currentPermissionClaims)
        {
            if (!targetPermissions.Contains(claim.Value))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
        }

        // Add new claims
        var existingClaimValues = currentPermissionClaims
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permission in targetPermissions)
        {
            if (!existingClaimValues.Contains(permission))
            {
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }
        }

        // Invalidate security stamps of all users holding this role so their tokens/claims sync
        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
        foreach (var user in usersInRole)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var userCount = await _context.UserRoles
            .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            Permissions = targetPermissions.OrderBy(p => p).ToList(),
            UserCount = userCount
        };
    }

    public async Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());
        if (role == null)
        {
            throw new NotFoundException($"Role with ID '{id}' was not found.");
        }

        if (string.Equals(role.Name, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("The SuperAdmin role cannot be deleted.");
        }

        var userCount = await _context.UserRoles
            .CountAsync(ur => ur.RoleId == role.Id, cancellationToken);

        if (userCount > 0)
        {
            throw new ValidationException($"Cannot delete role '{role.Name}' because it has {userCount} assigned user(s). Reassign or remove users first.");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => e.Description).ToList());
        }
    }

    public Task<IReadOnlyList<ModulePermissionsDto>> GetPermissionsGroupedByModuleAsync(CancellationToken cancellationToken = default)
    {
        var grouped = Permissions.GetPermissionsByModule();
        var dtos = grouped
            .Select(kvp => new ModulePermissionsDto
            {
                Module = kvp.Key,
                Permissions = kvp.Value.OrderBy(p => p).ToList()
            })
            .OrderBy(m => m.Module)
            .ToList();

        return Task.FromResult<IReadOnlyList<ModulePermissionsDto>>(dtos);
    }
}
