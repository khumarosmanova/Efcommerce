using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Helpers;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ProductDto>> GetFilteredAsync(
        ProductFilterParams filter,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<Product>()
            .Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        // 1. Filter: Active status (defaults to active only unless explicitly configured)
        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }
        else
        {
            query = query.Where(p => p.IsActive);
        }

        // 2. Filter: Search (Name, Description, SKU)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)) ||
                p.SKU.ToLower().Contains(search));
        }

        // 3. Filter: Category (including subcategories)
        if (filter.CategoryId.HasValue)
        {
            var targetCategoryIds = new List<int> { filter.CategoryId.Value };

            var subCategoryIds = await _unitOfWork.Repository<Category>()
                .Query()
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == filter.CategoryId.Value)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            targetCategoryIds.AddRange(subCategoryIds);
            query = query.Where(p => targetCategoryIds.Contains(p.CategoryId));
        }

        // 4. Filter: Price Range (based on effective price)
        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= filter.MaxPrice.Value);
        }

        // 5. Filter: In Stock
        if (filter.InStock == true)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        // 6. Sorting
        var sortKey = filter.SortBy?.ToLower()?.Trim();
        var isDescending = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        // Auto-detect direction embedded in SortBy (e.g., "price_desc", "price_asc", "name_asc", "name_desc")
        if (sortKey == "price_desc" || sortKey == "pricedesc")
        {
            sortKey = "price";
            isDescending = true;
        }
        else if (sortKey == "price_asc" || sortKey == "priceasc")
        {
            sortKey = "price";
            isDescending = false;
        }
        else if (sortKey == "name_desc" || sortKey == "namedesc")
        {
            sortKey = "name";
            isDescending = true;
        }
        else if (sortKey == "name_asc" || sortKey == "nameasc")
        {
            sortKey = "name";
            isDescending = false;
        }

        query = sortKey switch
        {
            "price" => isDescending
                ? query.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                : query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "name" => isDescending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name),
            "newest" => isDescending
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt),
            "popularity" => isDescending
                ? query.OrderBy(p => p.StockQuantity)
                : query.OrderByDescending(p => p.StockQuantity),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // 7. Pagination
        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
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
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                CategorySlug = p.Category != null ? p.Category.Slug : string.Empty,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, totalCount, page, pageSize);
    }

    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        return MapToDto(product);
    }

    public async Task<ProductDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>()
            .Query()
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException($"Product with slug '{slug}' was not found.");
        }

        return MapToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _unitOfWork.Repository<Category>()
            .ExistsAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new ValidationException($"Category with Id {dto.CategoryId} does not exist.");
        }

        var skuExists = await _unitOfWork.Repository<Product>()
            .ExistsAsync(p => p.SKU == dto.SKU, cancellationToken);
        if (skuExists)
        {
            throw new ValidationException($"A product with SKU '{dto.SKU}' already exists.");
        }

        var slug = await GenerateUniqueSlugAsync(dto.Slug, dto.Name, null, cancellationToken);

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            DiscountPrice = dto.DiscountPrice,
            StockQuantity = dto.StockQuantity,
            SKU = dto.SKU.Trim().ToUpperInvariant(),
            ImageUrl = dto.ImageUrl?.Trim(),
            CategoryId = dto.CategoryId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Product>().AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        var categoryExists = await _unitOfWork.Repository<Category>()
            .ExistsAsync(c => c.Id == dto.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new ValidationException($"Category with Id {dto.CategoryId} does not exist.");
        }

        var skuExists = await _unitOfWork.Repository<Product>()
            .ExistsAsync(p => p.SKU == dto.SKU && p.Id != id, cancellationToken);
        if (skuExists)
        {
            throw new ValidationException($"A product with SKU '{dto.SKU}' already exists.");
        }

        var slug = await GenerateUniqueSlugAsync(dto.Slug, dto.Name, id, cancellationToken);

        product.Name = dto.Name.Trim();
        product.Slug = slug;
        product.Description = dto.Description?.Trim();
        product.Price = dto.Price;
        product.DiscountPrice = dto.DiscountPrice;
        product.StockQuantity = dto.StockQuantity;
        product.SKU = dto.SKU.Trim().ToUpperInvariant();
        product.ImageUrl = dto.ImageUrl?.Trim();
        product.CategoryId = dto.CategoryId;
        product.IsActive = dto.IsActive;

        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(product.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        _unitOfWork.Repository<Product>().Delete(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
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
            CategoryName = product.Category?.Name ?? string.Empty,
            CategorySlug = product.Category?.Slug ?? string.Empty,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt
        };
    }

    private async Task<string> GenerateUniqueSlugAsync(
        string? specifiedSlug,
        string name,
        int? excludeId,
        CancellationToken cancellationToken)
    {
        var baseSlug = !string.IsNullOrWhiteSpace(specifiedSlug)
            ? SlugHelper.GenerateSlug(specifiedSlug)
            : SlugHelper.GenerateSlug(name);

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = "product";
        }

        var uniqueSlug = baseSlug;
        var suffix = 1;

        while (await _unitOfWork.Repository<Product>()
            .ExistsAsync(p => p.Slug == uniqueSlug && (!excludeId.HasValue || p.Id != excludeId.Value), cancellationToken))
        {
            uniqueSlug = $"{baseSlug}-{suffix++}";
        }

        return uniqueSlug;
    }
}
