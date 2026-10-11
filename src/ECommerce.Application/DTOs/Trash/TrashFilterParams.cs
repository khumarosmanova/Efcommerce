namespace ECommerce.Application.DTOs.Trash;

public class TrashFilterParams
{
    public string? Search { get; set; }
    public string? SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
