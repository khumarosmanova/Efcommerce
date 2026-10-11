namespace ECommerce.Application.DTOs.Statistics;

public class SalesQueryParameters
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? GroupBy { get; set; } = "day";
}
