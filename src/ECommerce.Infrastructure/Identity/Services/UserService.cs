using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Users;
using ECommerce.Application.Interfaces;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Identity.Services;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        AppDbContext context,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<UserManagementDto>> GetUsersAsync(
        UserFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
        {
            var roleName = filter.Role.Trim();
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var userIdsWithRole = _context.UserRoles
                    .Where(ur => ur.RoleId == role.Id)
                    .Select(ur => ur.UserId);

                query = query.Where(u => userIdsWithRole.Contains(u.Id));
            }
            else
            {
                return new PagedResult<UserManagementDto>(new List<UserManagementDto>(), 0, filter.Page, filter.PageSize);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<UserManagementDto>();
        foreach (var user in users)
        {
            var roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
            var permissions = await GetPermissionsForRolesAsync(roles);
            var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            dtos.Add(new UserManagementDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                Permissions = permissions,
                IsLockedOut = isLocked,
                CreatedAt = user.CreatedAt
            });
        }

        return new PagedResult<UserManagementDto>(dtos, totalCount, page, pageSize);
    }

    public async Task<UserManagementDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new NotFoundException($"User with ID '{id}' was not found.");
        }

        var roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
        var permissions = await GetPermissionsForRolesAsync(roles);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        return new UserManagementDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            Permissions = permissions,
            IsLockedOut = isLocked,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserManagementDto> UpdateUserRolesAsync(
        Guid id,
        UpdateUserRolesDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new NotFoundException($"User with ID '{id}' was not found.");
        }

        // Validate each role requested
        var targetRoles = dto.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (targetRoles.Count == 0)
        {
            throw new ValidationException("User must be assigned at least one role.");
        }

        foreach (var roleName in targetRoles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                throw new ValidationException($"Role '{roleName}' does not exist.");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var isTargetSuperAdmin = currentRoles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);
        var willBeSuperAdmin = targetRoles.Contains("SuperAdmin", StringComparer.OrdinalIgnoreCase);

        // Rule: Prevent a user from removing their own SuperAdmin role
        var currentUserId = _currentUserService.UserId;
        var isSelf = !string.IsNullOrEmpty(currentUserId) &&
                     string.Equals(currentUserId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase);

        if (isSelf && isTargetSuperAdmin && !willBeSuperAdmin)
        {
            throw new ValidationException("You cannot remove your own SuperAdmin role.");
        }

        // Rule: Prevent removing SuperAdmin if this is the only SuperAdmin in the system
        if (isTargetSuperAdmin && !willBeSuperAdmin)
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            if (superAdmins.Count <= 1)
            {
                throw new ValidationException("Cannot remove SuperAdmin role because this user is the only SuperAdmin in the system.");
            }
        }

        // Synchronize roles
        var rolesToRemove = currentRoles.Except(targetRoles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToAdd = targetRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                throw new ValidationException(removeResult.Errors.Select(e => e.Description).ToList());
            }
        }

        if (rolesToAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                throw new ValidationException(addResult.Errors.Select(e => e.Description).ToList());
            }
        }

        // Invalidate tokens / synchronize security stamp
        await _userManager.UpdateSecurityStampAsync(user);

        // Revoke active refresh tokens so user re-authenticates or requests fresh claims
        var activeTokens = await _context.RefreshTokens
            .Where(r => r.UserId == user.Id && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var updatedRoles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
        var permissions = await GetPermissionsForRolesAsync(updatedRoles);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        return new UserManagementDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = updatedRoles,
            Permissions = permissions,
            IsLockedOut = isLocked,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserManagementDto> UpdateUserStatusAsync(
        Guid id,
        UpdateUserStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            throw new NotFoundException($"User with ID '{id}' was not found.");
        }

        var currentUserId = _currentUserService.UserId;
        var isSelf = !string.IsNullOrEmpty(currentUserId) &&
                     string.Equals(currentUserId, user.Id.ToString(), StringComparison.OrdinalIgnoreCase);

        // Rule: Prevent locking self
        if (isSelf && dto.IsLocked)
        {
            throw new ValidationException("You cannot lock your own account.");
        }

        // Rule: Prevent locking the sole active SuperAdmin
        if (dto.IsLocked && await _userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
            var otherActiveSuperAdmins = superAdmins
                .Where(u => u.Id != user.Id && !(u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow))
                .ToList();

            if (otherActiveSuperAdmins.Count == 0)
            {
                throw new ValidationException("Cannot lock this user because they are the only active SuperAdmin in the system.");
            }
        }

        if (dto.IsLocked)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));

            // Immediately revoke all active refresh tokens and update security stamp
            await _userManager.UpdateSecurityStampAsync(user);

            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == user.Id && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var roles = (await _userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
        var permissions = await GetPermissionsForRolesAsync(roles);
        var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

        return new UserManagementDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles,
            Permissions = permissions,
            IsLockedOut = isLocked,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task<List<string>> GetPermissionsForRolesAsync(IEnumerable<string> roles)
    {
        var permissionSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var claims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in claims.Where(c => c.Type == "permission"))
                {
                    if (!string.IsNullOrWhiteSpace(claim.Value))
                    {
                        permissionSet.Add(claim.Value);
                    }
                }
            }
        }

        return permissionSet.OrderBy(p => p).ToList();
    }
}
