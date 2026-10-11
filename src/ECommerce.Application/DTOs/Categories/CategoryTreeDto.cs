namespace ECommerce.Application.DTOs.Categories;

public class CategoryTreeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
    public List<CategoryTreeDto> SubCategories { get; set; } = new();
}
