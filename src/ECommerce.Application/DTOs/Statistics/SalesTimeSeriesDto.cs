namespace ECommerce.Application.DTOs.Statistics;

public class SalesTimeSeriesDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
