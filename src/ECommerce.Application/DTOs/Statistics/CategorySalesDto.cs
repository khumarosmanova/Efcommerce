namespace ECommerce.Application.DTOs.Statistics;

public class CategorySalesDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public double RevenuePercentage { get; set; }
}
