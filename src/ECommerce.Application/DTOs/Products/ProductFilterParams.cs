namespace ECommerce.Application.DTOs.Products;

public class ProductFilterParams
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStock { get; set; }
    public string? SortBy { get; set; } // "price", "name", "newest", "popularity"
    public string? SortDirection { get; set; } // "asc", "desc"
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    // Used by admin queries to filter by active status or view inactive items
    public bool? IsActive { get; set; }
}
