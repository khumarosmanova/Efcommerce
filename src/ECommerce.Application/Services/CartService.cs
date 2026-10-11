using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.DTOs.Cart;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CartService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    private bool IsAdminUser() =>
        _currentUserService.Roles.Any(r => r is "SuperAdmin" or "Admin" or "Moderator");

    private string GetRequiredUserId()
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("User is not authenticated.");
        }
        return userId;
    }

    public async Task<CartDto> GetCartAsync(CancellationToken cancellationToken = default)
    {
        if (IsAdminUser())
        {
            return new CartDto { Items = new List<CartItemDto>() };
        }
        var userId = GetRequiredUserId();
        var cart = await GetOrCreateCartEntityAsync(userId, cancellationToken);
        return MapToDto(cart);
    }

    public async Task<CartDto> AddItemAsync(AddToCartDto dto, CancellationToken cancellationToken = default)
    {
        if (IsAdminUser())
        {
            throw new ValidationException("Administrator accounts cannot add items to cart or place orders.");
        }
        var userId = GetRequiredUserId();

        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId, cancellationToken);

        if (product == null || !product.IsActive)
        {
            throw new ValidationException("Product is unavailable or does not exist.");
        }

        var cart = await GetOrCreateCartEntityAsync(userId, cancellationToken);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + dto.Quantity;
            if (product.StockQuantity < newQuantity)
            {
                throw new ValidationException(
                    $"Cannot add {dto.Quantity} item(s). Only {product.StockQuantity} available in stock (you already have {existingItem.Quantity} in cart).");
            }

            existingItem.Quantity = newQuantity;
            _unitOfWork.Repository<CartItem>().Update(existingItem);
        }
        else
        {
            if (product.StockQuantity < dto.Quantity)
            {
                throw new ValidationException(
                    $"Requested quantity ({dto.Quantity}) exceeds available stock ({product.StockQuantity}).");
            }

            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = dto.Quantity
            };
            await _unitOfWork.Repository<CartItem>().AddAsync(newItem, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetCartAsync(cancellationToken);
    }

    public async Task<CartDto> UpdateItemAsync(int itemId, UpdateCartItemDto dto, CancellationToken cancellationToken = default)
    {
        if (IsAdminUser())
        {
            throw new ValidationException("Administrator accounts cannot modify shopping cart.");
        }
        var userId = GetRequiredUserId();
        var cart = await GetOrCreateCartEntityAsync(userId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            throw new NotFoundException("Cart item", itemId);
        }

        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .FirstOrDefaultAsync(p => p.Id == item.ProductId, cancellationToken);

        if (product == null || !product.IsActive)
        {
            throw new ValidationException("Product is unavailable or has been removed.");
        }

        if (product.StockQuantity < dto.Quantity)
        {
            throw new ValidationException(
                $"Requested quantity ({dto.Quantity}) exceeds available stock ({product.StockQuantity}).");
        }

        item.Quantity = dto.Quantity;
        _unitOfWork.Repository<CartItem>().Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetCartAsync(cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        if (IsAdminUser())
        {
            throw new ValidationException("Administrator accounts cannot modify shopping cart.");
        }
        var userId = GetRequiredUserId();
        var cart = await GetOrCreateCartEntityAsync(userId, cancellationToken);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _unitOfWork.Repository<CartItem>().Delete(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await GetCartAsync(cancellationToken);
    }

    public async Task ClearCartAsync(CancellationToken cancellationToken = default)
    {
        if (IsAdminUser())
        {
            return;
        }
        var userId = GetRequiredUserId();
        var cart = await GetOrCreateCartEntityAsync(userId, cancellationToken);

        foreach (var item in cart.Items.ToList())
        {
            _unitOfWork.Repository<CartItem>().Delete(item);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Cart> GetOrCreateCartEntityAsync(string userId, CancellationToken cancellationToken)
    {
        var cart = await _unitOfWork.Repository<Cart>()
            .Query()
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Cart>().AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return cart;
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.Items
            .Where(i => i.Product != null && !i.Product.IsDeleted)
            .Select(i => new CartItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product!.Name,
                ProductSlug = i.Product.Slug,
                ProductImageUrl = i.Product.ImageUrl,
                UnitPrice = i.Product.DiscountPrice ?? i.Product.Price,
                Quantity = i.Quantity,
                AvailableStock = i.Product.StockQuantity
            })
            .ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = items
        };
    }
}
