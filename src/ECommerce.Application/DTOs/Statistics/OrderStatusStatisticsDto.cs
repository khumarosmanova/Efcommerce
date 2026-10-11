namespace ECommerce.Application.DTOs.Statistics;

public class OrderStatusStatisticsDto
{
    public string Status { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalAmount { get; set; }
    public double Percentage { get; set; }
}
