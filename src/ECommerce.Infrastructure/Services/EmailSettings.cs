namespace ECommerce.Infrastructure.Services;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public bool EnableSmtp { get; set; } = false;
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@shophub.az";
    public string FromName { get; set; } = "ShopHub Store";
}
