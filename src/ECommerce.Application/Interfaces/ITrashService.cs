using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Trash;

namespace ECommerce.Application.Interfaces;

public interface ITrashService
{
    Task<PagedResult<TrashProductDto>> GetDeletedProductsAsync(TrashFilterParams filter, CancellationToken cancellationToken = default);
    Task<PagedResult<TrashCategoryDto>> GetDeletedCategoriesAsync(TrashFilterParams filter, CancellationToken cancellationToken = default);
    Task<TrashProductDto> RestoreProductAsync(int id, CancellationToken cancellationToken = default);
    Task<TrashCategoryDto> RestoreCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task HardDeleteProductAsync(int id, CancellationToken cancellationToken = default);
    Task HardDeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
