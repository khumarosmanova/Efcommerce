using ECommerce.Application.DTOs.Roles;

namespace ECommerce.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateRoleAsync(CreateRoleDto dto, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateRoleAsync(Guid id, UpdateRoleDto dto, CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModulePermissionsDto>> GetPermissionsGroupedByModuleAsync(CancellationToken cancellationToken = default);
}
