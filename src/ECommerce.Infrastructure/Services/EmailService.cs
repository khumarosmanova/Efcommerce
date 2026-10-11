using System.Net;
using System.Net.Mail;
using System.Text;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> options, ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to)) return;

        if (_settings.EnableSmtp && !string.IsNullOrWhiteSpace(_settings.Host))
        {
            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8
                };
                message.To.Add(to);

                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("SMTP Email sent successfully to {ToEmail} with subject '{Subject}'", to, subject);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via SMTP to {ToEmail}. Falling back to simulation log.", to);
            }
        }

        // Development fallback / Simulation logger
        _logger.LogInformation(
            "=================== [EMAIL NOTIFICATION MOCK] ===================\n" +
            "TO: {ToEmail}\n" +
            "FROM: {FromName} <{FromEmail}>\n" +
            "SUBJECT: {Subject}\n" +
            "BODY (Snippet): {BodySnippet}\n" +
            "==================================================================",
            to, _settings.FromName, _settings.FromEmail, subject,
            htmlBody.Length > 200 ? htmlBody[..200] + "..." : htmlBody);
    }

    public async Task SendWelcomeEmailAsync(string to, string fullName, CancellationToken cancellationToken = default)
    {
        var subject = "ShopHub-a Xoş Gəlmisiniz! 🎉 | Welcome to ShopHub";
        var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>Welcome to ShopHub</title></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #1e293b;'>
  <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
    <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 32px 24px; text-align: center; color: white;'>
      <h1 style='margin: 0; font-size: 26px; font-weight: 800;'>ShopHub</h1>
      <p style='margin: 8px 0 0 0; opacity: 0.9; font-size: 15px;'>Müasir onlayn alış-veriş platformanız</p>
    </div>
    <div style='padding: 32px 24px;'>
      <h2 style='font-size: 20px; color: #0f172a; margin-top: 0;'>Hörmətli {WebUtility.HtmlEncode(fullName)},</h2>
      <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
        ShopHub ailəsinə qoşulduğunuz üçün təşəkkür edirik! Hesabınız uğurla yaradıldı.
      </p>
      <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
        İndi siz minlərlə məhsulu nəzərdən keçirə, bəyəndiyiniz məhsulları sevimlilərə (wishlist) əlavə edə, reytinq və rəylər bildirə, və sərfəli endirimlərlə sifariş verə bilərsiniz.
      </p>
      <div style='text-align: center; margin: 32px 0;'>
        <a href='http://localhost:5173' style='display: inline-block; background-color: #10b981; color: white; padding: 14px 28px; border-radius: 10px; font-weight: 600; text-decoration: none; font-size: 15px;'>
          Alış-verişə Başla / Start Shopping
        </a>
      </div>
      <p style='font-size: 13px; color: #94a3b8; border-top: 1px solid #f1f5f9; padding-top: 16px;'>
        Bu email avtomatik olaraq ShopHub sistemi tərəfindən göndərilmişdir.
      </p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendOrderConfirmationEmailAsync(
        string to,
        string orderNumber,
        decimal totalAmount,
        string shippingAddress,
        IReadOnlyList<(string ProductName, int Quantity, decimal UnitPrice, decimal TotalPrice)> items,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Sifarişiniz Təsdiqləndi! #{orderNumber} | ShopHub Order Confirmation";

        var itemsHtml = new StringBuilder();
        foreach (var item in items)
        {
            itemsHtml.Append($@"
              <tr style='border-bottom: 1px solid #f1f5f9;'>
                <td style='padding: 12px 8px; font-size: 14px; color: #1e293b; font-weight: 500;'>{WebUtility.HtmlEncode(item.ProductName)}</td>
                <td style='padding: 12px 8px; font-size: 14px; color: #64748b; text-align: center;'>{item.Quantity} ədəd</td>
                <td style='padding: 12px 8px; font-size: 14px; color: #64748b; text-align: right;'>${item.UnitPrice:N2}</td>
                <td style='padding: 12px 8px; font-size: 14px; color: #0f172a; font-weight: 600; text-align: right;'>${item.TotalPrice:N2}</td>
              </tr>");
        }

        var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>Order #{orderNumber} Confirmation</title></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #1e293b;'>
  <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
    <div style='background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 32px 24px; text-align: center; color: white;'>
      <h1 style='margin: 0; font-size: 24px; font-weight: 800;'>Sifarişiniz Qəbul Olundu! 🎉</h1>
      <p style='margin: 8px 0 0 0; opacity: 0.95; font-size: 14px;'>Sifariş nömrəsi: #{WebUtility.HtmlEncode(orderNumber)}</p>
    </div>
    <div style='padding: 32px 24px;'>
      <p style='font-size: 15px; color: #475569; margin-top: 0;'>
        Sifarişiniz uğurla qeydiyyata alındı və hazırlanma mərhələsindədir.
      </p>

      <div style='background: #f8fafc; border-radius: 12px; padding: 16px; margin-bottom: 24px; border: 1px solid #e2e8f0;'>
        <p style='margin: 0 0 6px 0; font-size: 12px; font-weight: 700; text-transform: uppercase; color: #94a3b8;'>Çatdırılma Ünvanı</p>
        <p style='margin: 0; font-size: 14px; color: #1e293b; font-weight: 500;'>{WebUtility.HtmlEncode(shippingAddress)}</p>
      </div>

      <table style='width: 100%; border-collapse: collapse; margin-bottom: 24px;'>
        <thead>
          <tr style='border-bottom: 2px solid #e2e8f0; color: #64748b; font-size: 12px; text-transform: uppercase; font-weight: 600;'>
            <th style='text-align: left; padding: 8px;'>Məhsul</th>
            <th style='text-align: center; padding: 8px;'>Say</th>
            <th style='text-align: right; padding: 8px;'>Qiymət</th>
            <th style='text-align: right; padding: 8px;'>Cəm</th>
          </tr>
        </thead>
        <tbody>
          {itemsHtml}
        </tbody>
        <tfoot>
          <tr>
            <td colspan='3' style='padding: 16px 8px 8px 8px; text-align: right; font-size: 15px; font-weight: 700; color: #0f172a;'>Ümumi Məbləğ:</td>
            <td style='padding: 16px 8px 8px 8px; text-align: right; font-size: 18px; font-weight: 800; color: #10b981;'>${totalAmount:N2}</td>
          </tr>
        </tfoot>
      </table>

      <div style='text-align: center; margin-top: 24px;'>
        <a href='http://localhost:5173/orders' style='display: inline-block; background-color: #0f172a; color: white; padding: 12px 24px; border-radius: 10px; font-weight: 600; text-decoration: none; font-size: 14px;'>
          Sifarişlərimə Bax / View Orders
        </a>
      </div>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task SendOrderStatusChangedEmailAsync(
        string to,
        string orderNumber,
        string status,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Sifariş #{orderNumber} Statusu Yeniləndi: {status} | ShopHub";
        var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>Order Status Update</title></head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #1e293b;'>
  <div style='max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
    <div style='background: #0f172a; padding: 24px; text-align: center; color: white;'>
      <h2 style='margin: 0; font-size: 22px;'>ShopHub Sifariş Yeniləməsi</h2>
    </div>
    <div style='padding: 32px 24px;'>
      <p style='font-size: 15px; color: #475569;'>
        #{WebUtility.HtmlEncode(orderNumber)} nömrəli sifarişinizin hazırki statusu:
      </p>
      <div style='display: inline-block; background-color: #ecfdf5; color: #047857; border: 1px solid #a7f3d0; padding: 8px 18px; border-radius: 9999px; font-weight: 700; font-size: 15px; margin: 12px 0 20px 0;'>
        {WebUtility.HtmlEncode(status)}
      </div>
      <p style='font-size: 14px; color: #64748b; line-height: 1.5;'>
        Sifarişinizin gedişatını istənilən vaxt şəxsi kabinetinizdəki Sifarişlərim bölməsindən izləyə bilərsiniz.
      </p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(to, subject, body, cancellationToken);
    }
}
