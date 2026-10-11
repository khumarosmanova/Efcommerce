using AppValidationException = ECommerce.Application.Common.Exceptions.ValidationException;
using ECommerce.Application.Common.Models;
using ECommerce.Application.DTOs.Products;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;
    private readonly IWebHostEnvironment _environment;

    public ProductsController(
        IProductService productService,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator,
        IWebHostEnvironment environment)
    {
        _productService = productService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _environment = environment;
    }

    /// <summary>
    /// Retrieves paginated catalog of active products with server-side filtering and sorting.
    /// Category filter automatically includes all subcategories.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductFilterParams filter,
        CancellationToken cancellationToken)
    {
        // Public storefront only browses active items
        filter.IsActive = true;
        var result = await _productService.GetFilteredAsync(filter, cancellationToken);
        return Success(result, "Products retrieved successfully.");
    }

    /// <summary>
    /// Admin endpoint to retrieve products (active or inactive) with full filtering and pagination.
    /// </summary>
    [HttpGet("admin")]
    [HasPermission(Permissions.Product.Manage)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProductsAdmin(
        [FromQuery] ProductFilterParams filter,
        CancellationToken cancellationToken)
    {
        var result = await _productService.GetFilteredAsync(filter, cancellationToken);
        return Success(result, "Admin products retrieved successfully.");
    }

    /// <summary>
    /// Retrieves a single product by its unique integer ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _productService.GetByIdAsync(id, cancellationToken);
        return Success(result, "Product retrieved successfully.");
    }

    /// <summary>
    /// Retrieves a single product by its unique SEO slug.
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _productService.GetBySlugAsync(slug, cancellationToken);
        return Success(result, "Product retrieved successfully.");
    }

    /// <summary>
    /// Creates a new product. Slug is generated uniquely and automatically if omitted.
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Product.Manage)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _productService.CreateAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<ProductDto>.Success(result, "Product created successfully."));
    }

    /// <summary>
    /// Updates an existing product by its ID.
    /// </summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Product.Manage)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new AppValidationException(validationResult.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var result = await _productService.UpdateAsync(id, dto, cancellationToken);
        return Success(result, "Product updated successfully.");
    }

    /// <summary>
    /// Soft-deletes a product. Never exposes or deletes historical order lines.
    /// </summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Product.Manage)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return Success("Product soft-deleted successfully.");
    }

    /// <summary>
    /// Uploads a product image file into wwwroot/uploads/products with validation on file size and extension.
    /// </summary>
    [HttpPost("upload-image")]
    [HasPermission(Permissions.Product.Manage)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadImage([FromForm] ProductImageUploadRequest request)
    {
        var file = request?.File;
        if (file == null || file.Length == 0)
        {
            throw new AppValidationException("Please select an image file to upload.");
        }

        // 1. Validate file size (max 5 MB)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            throw new AppValidationException("Image file size cannot exceed 5 MB.");
        }

        // 2. Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        if (!allowedExtensions.Contains(extension))
        {
            throw new AppValidationException("Invalid image file format. Allowed formats: .jpg, .jpeg, .png, .webp, .gif.");
        }

        // 3. Save file into wwwroot/uploads/products
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "products");

        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/products/{uniqueFileName}";
        return Success(relativeUrl, "Image uploaded successfully.");
    }
}

public class ProductImageUploadRequest
{
    public IFormFile File { get; set; } = null!;
}

