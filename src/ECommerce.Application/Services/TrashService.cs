using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Trash;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class TrashService : ITrashService
{
    private readonly IUnitOfWork _unitOfWork;

    public TrashService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<TrashProductDto>> GetDeletedProductsAsync(
        TrashFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Product>()
            .Query()
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                p.SKU.ToLower().Contains(term));
        }

        query = query.Include(p => p.Category);

        if (string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderBy(p => p.DeletedAt);
        }
        else
        {
            query = query.OrderByDescending(p => p.DeletedAt);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new TrashProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                StockQuantity = p.StockQuantity,
                SKU = p.SKU,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                IsCategoryDeleted = p.Category != null && p.Category.IsDeleted,
                IsActive = p.IsActive,
                DeletedAt = p.DeletedAt,
                DeletedBy = p.DeletedBy,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TrashProductDto>(items, totalCount, page, pageSize);
    }

    public async Task<PagedResult<TrashCategoryDto>> GetDeletedCategoriesAsync(
        TrashFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Category>()
            .Query()
            .IgnoreQueryFilters()
            .Where(c => c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        query = query.Include(c => c.ParentCategory).Include(c => c.Products);

        if (string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase))
        {
            query = query.OrderBy(c => c.DeletedAt);
        }
        else
        {
            query = query.OrderByDescending(c => c.DeletedAt);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new TrashCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                ProductCount = c.Products.Count,
                DeletedAt = c.DeletedAt,
                DeletedBy = c.DeletedBy,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TrashCategoryDto>(items, totalCount, page, pageSize);
    }

    public async Task<TrashProductDto> RestoreProductAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .IgnoreQueryFilters()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null || !product.IsDeleted)
        {
            throw new NotFoundException("Deleted product", id);
        }

        // Rule: When restoring a product whose category is deleted, return a clear validation error asking to restore the category first
        var category = await _unitOfWork.Repository<Category>()
            .Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == product.CategoryId, cancellationToken);

        if (category == null || category.IsDeleted)
        {
            throw new ValidationException(
                $"Cannot restore product '{product.Name}' because its category '{category?.Name ?? "Unknown"}' is currently deleted. Please restore the category first.");
        }

        _unitOfWork.Repository<Product>().Restore(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TrashProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            StockQuantity = product.StockQuantity,
            SKU = product.SKU,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            IsCategoryDeleted = false,
            IsActive = product.IsActive,
            DeletedAt = null,
            DeletedBy = null,
            CreatedAt = product.CreatedAt
        };
    }

    public async Task<TrashCategoryDto> RestoreCategoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>()
            .Query()
            .IgnoreQueryFilters()
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null || !category.IsDeleted)
        {
            throw new NotFoundException("Deleted category", id);
        }

        if (category.ParentCategoryId.HasValue)
        {
            var parent = await _unitOfWork.Repository<Category>()
                .Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == category.ParentCategoryId.Value, cancellationToken);

            if (parent != null && parent.IsDeleted)
            {
                throw new ValidationException(
                    $"Cannot restore category '{category.Name}' because its parent category '{parent.Name}' is currently deleted. Please restore the parent category first.");
            }
        }

        _unitOfWork.Repository<Category>().Restore(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TrashCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            ProductCount = 0,
            DeletedAt = null,
            DeletedBy = null,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task HardDeleteProductAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        // Rule: Block hard delete if the product is referenced by any OrderItem
        var hasOrderItems = await _unitOfWork.Repository<OrderItem>()
            .Query()
            .AnyAsync(oi => oi.ProductId == id, cancellationToken);

        if (hasOrderItems)
        {
            throw new ValidationException(
                $"Cannot permanently delete product '{product.Name}' (ID: {product.Id}) because it is referenced in past customer orders. Products with order history must be preserved for financial and audit records.");
        }

        // Clean up any cart items with this product
        var cartItems = await _unitOfWork.Repository<CartItem>()
            .Query()
            .Where(ci => ci.ProductId == id)
            .ToListAsync(cancellationToken);

        foreach (var item in cartItems)
        {
            _unitOfWork.Repository<CartItem>().Delete(item);
        }

        _unitOfWork.Repository<Product>().HardDelete(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteCategoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>()
            .Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        // Rule: Block hard delete if any product still references it (whether deleted or active)
        var referencingProductsCount = await _unitOfWork.Repository<Product>()
            .Query()
            .IgnoreQueryFilters()
            .CountAsync(p => p.CategoryId == id, cancellationToken);

        if (referencingProductsCount > 0)
        {
            throw new ValidationException(
                $"Cannot permanently delete category '{category.Name}' (ID: {category.Id}) because {referencingProductsCount} product(s) still reference it. Reassign or permanently delete those products first.");
        }

        // Rule: Block hard delete if any subcategories reference it
        var referencingSubcategoriesCount = await _unitOfWork.Repository<Category>()
            .Query()
            .IgnoreQueryFilters()
            .CountAsync(c => c.ParentCategoryId == id, cancellationToken);

        if (referencingSubcategoriesCount > 0)
        {
            throw new ValidationException(
                $"Cannot permanently delete category '{category.Name}' (ID: {category.Id}) because {referencingSubcategoriesCount} subcategory(ies) reference it as parent category. Reassign or delete the subcategories first.");
        }

        _unitOfWork.Repository<Category>().HardDelete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
