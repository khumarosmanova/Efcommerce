using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Users;

namespace ECommerce.Application.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserManagementDto>> GetUsersAsync(UserFilterParams filter, CancellationToken cancellationToken = default);
    Task<UserManagementDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserManagementDto> UpdateUserRolesAsync(Guid id, UpdateUserRolesDto dto, CancellationToken cancellationToken = default);
    Task<UserManagementDto> UpdateUserStatusAsync(Guid id, UpdateUserStatusDto dto, CancellationToken cancellationToken = default);
}
