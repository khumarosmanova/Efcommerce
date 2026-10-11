namespace ECommerce.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string to, string fullName, CancellationToken cancellationToken = default);
    Task SendOrderConfirmationEmailAsync(
        string to,
        string orderNumber,
        decimal totalAmount,
        string shippingAddress,
        IReadOnlyList<(string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice)> items,
        CancellationToken cancellationToken = default);
    Task SendOrderStatusChangedEmailAsync(string to, string orderNumber, string status, CancellationToken cancellationToken = default);
}
