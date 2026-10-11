using ECommerce.Application.DTOs.Products;

namespace ECommerce.Application.Interfaces;

public interface IWishlistService
{
    Task<IReadOnlyList<ProductDto>> GetWishlistAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetWishlistProductIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ToggleWishlistAsync(Guid userId, int productId, CancellationToken cancellationToken = default);
    Task AddToWishlistAsync(Guid userId, int productId, CancellationToken cancellationToken = default);
    Task RemoveFromWishlistAsync(Guid userId, int productId, CancellationToken cancellationToken = default);
}
