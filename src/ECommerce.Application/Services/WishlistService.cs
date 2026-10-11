using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class WishlistService : IWishlistService
{
    private readonly IUnitOfWork _unitOfWork;

    public WishlistService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ProductDto>> GetWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wishlistItems = await _unitOfWork.Repository<WishlistItem>()
            .Query()
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
                .ThenInclude(p => p!.Category)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        return wishlistItems
            .Where(w => w.Product != null && !w.Product.IsDeleted && w.Product.IsActive)
            .Select(w => new ProductDto
            {
                Id = w.Product!.Id,
                Name = w.Product.Name,
                Slug = w.Product.Slug,
                Description = w.Product.Description,
                Price = w.Product.Price,
                DiscountPrice = w.Product.DiscountPrice,
                StockQuantity = w.Product.StockQuantity,
                SKU = w.Product.SKU,
                ImageUrl = w.Product.ImageUrl,
                CategoryId = w.Product.CategoryId,
                CategoryName = w.Product.Category?.Name ?? string.Empty,
                CategorySlug = w.Product.Category?.Slug ?? string.Empty,
                IsActive = w.Product.IsActive,
                CreatedAt = w.Product.CreatedAt
            })
            .ToList();
    }

    public async Task<IReadOnlyList<int>> GetWishlistProductIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<WishlistItem>()
            .Query()
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.ProductId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ToggleWishlistAsync(Guid userId, int productId, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Repository<WishlistItem>()
            .Query()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

        if (existing != null)
        {
            _unitOfWork.Repository<WishlistItem>().Delete(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false; // Removed
        }

        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {productId} was not found.");
        }

        var newItem = new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<WishlistItem>().AddAsync(newItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true; // Added
    }

    public async Task AddToWishlistAsync(Guid userId, int productId, CancellationToken cancellationToken = default)
    {
        var exists = await _unitOfWork.Repository<WishlistItem>()
            .ExistsAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

        if (exists) return;

        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException($"Product with ID {productId} was not found.");
        }

        var item = new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<WishlistItem>().AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromWishlistAsync(Guid userId, int productId, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Repository<WishlistItem>()
            .Query()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

        if (item != null)
        {
            _unitOfWork.Repository<WishlistItem>().Delete(item);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
