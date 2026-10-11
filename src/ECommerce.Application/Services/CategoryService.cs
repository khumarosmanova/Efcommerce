using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Helpers;
using ECommerce.Application.DTOs.Categories;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Repository<Category>()
            .Query()
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description,
            ImageUrl = c.ImageUrl,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategory?.Name,
            ProductCount = c.Products.Count(p => !p.IsDeleted),
            CreatedAt = c.CreatedAt
        }).ToList();
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var allCategories = await _unitOfWork.Repository<Category>()
            .Query()
            .AsNoTracking()
            .Include(c => c.Products)
            .ToListAsync(cancellationToken);

        var rootCategories = allCategories
            .Where(c => c.ParentCategoryId == null)
            .OrderBy(c => c.Name)
            .Select(c => MapToTreeDto(c, allCategories))
            .ToList();

        return rootCategories;
    }

    public async Task<CategoryDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>()
            .Query()
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            ProductCount = category.Products.Count(p => !p.IsDeleted),
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>()
            .Query()
            .AsNoTracking()
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException($"Category with slug '{slug}' was not found.");
        }

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ParentCategoryId = category.ParentCategoryId,
            ParentCategoryName = category.ParentCategory?.Name,
            ProductCount = category.Products.Count(p => !p.IsDeleted),
            CreatedAt = category.CreatedAt
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.ParentCategoryId.HasValue)
        {
            var parentExists = await _unitOfWork.Repository<Category>()
                .ExistsAsync(c => c.Id == dto.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new ValidationException($"Parent category with Id {dto.ParentCategoryId.Value} does not exist.");
            }
        }

        var slug = await GenerateUniqueSlugAsync(dto.Slug, dto.Name, null, cancellationToken);

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim(),
            ImageUrl = dto.ImageUrl?.Trim(),
            ParentCategoryId = dto.ParentCategoryId
        };

        await _unitOfWork.Repository<Category>().AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(category.Id, cancellationToken);
    }

    public async Task<CategoryDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        if (dto.ParentCategoryId.HasValue)
        {
            if (dto.ParentCategoryId.Value == id)
            {
                throw new ValidationException("A category cannot be its own parent.");
            }

            var parentExists = await _unitOfWork.Repository<Category>()
                .ExistsAsync(c => c.Id == dto.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
            {
                throw new ValidationException($"Parent category with Id {dto.ParentCategoryId.Value} does not exist.");
            }
        }

        var slug = await GenerateUniqueSlugAsync(dto.Slug, dto.Name, id, cancellationToken);

        category.Name = dto.Name.Trim();
        category.Slug = slug;
        category.Description = dto.Description?.Trim();
        category.ImageUrl = dto.ImageUrl?.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;

        _unitOfWork.Repository<Category>().Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(category.Id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        _unitOfWork.Repository<Category>().Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static CategoryTreeDto MapToTreeDto(Category category, List<Category> allCategories)
    {
        var subCategories = allCategories
            .Where(c => c.ParentCategoryId == category.Id)
            .OrderBy(c => c.Name)
            .Select(c => MapToTreeDto(c, allCategories))
            .ToList();

        return new CategoryTreeDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            ProductCount = category.Products.Count(p => !p.IsDeleted),
            SubCategories = subCategories
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
            baseSlug = "category";
        }

        var uniqueSlug = baseSlug;
        var suffix = 1;

        while (await _unitOfWork.Repository<Category>()
            .ExistsAsync(c => c.Slug == uniqueSlug && (!excludeId.HasValue || c.Id != excludeId.Value), cancellationToken))
        {
            uniqueSlug = $"{baseSlug}-{suffix++}";
        }

        return uniqueSlug;
    }
}
