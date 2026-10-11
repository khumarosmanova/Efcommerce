using ECommerce.Application.DTOs.Cart;

namespace ECommerce.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(CancellationToken cancellationToken = default);
    Task<CartDto> AddItemAsync(AddToCartDto dto, CancellationToken cancellationToken = default);
    Task<CartDto> UpdateItemAsync(int itemId, UpdateCartItemDto dto, CancellationToken cancellationToken = default);
    Task<CartDto> RemoveItemAsync(int itemId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(CancellationToken cancellationToken = default);
}
