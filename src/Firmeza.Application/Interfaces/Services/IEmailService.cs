using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task SendSaleReceiptAsync(SaleDto sale, string customerEmail, CancellationToken cancellationToken = default);
    Task SendWelcomeEmailAsync(string customerEmail, string customerName, CancellationToken cancellationToken = default);
}
