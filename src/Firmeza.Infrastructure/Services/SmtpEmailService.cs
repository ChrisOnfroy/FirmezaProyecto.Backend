using System.Net;
using System.Net.Mail;
using System.Text;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Firmeza.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_settings.Password))
            {
                _logger.LogWarning(
                    "⚠️ [SMTP Gmail Simulado]: Las credenciales de SMTP no han sido configuradas en appsettings.json. Correo dirigido a '{To}' con asunto '{Subject}' simulado correctamente.",
                    toEmail,
                    subject);
                return;
            }

            var fromAddress = new MailAddress(_settings.SenderEmail, _settings.SenderName);
            var toAddress = new MailAddress(toEmail);

            using var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    string.IsNullOrWhiteSpace(_settings.Username) ? _settings.SenderEmail : _settings.Username,
                    _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("📧 [SMTP Real Gmail]: Correo enviado con éxito a {To}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al enviar correo SMTP a {To}", toEmail);
            // No bloqueamos la transacción del usuario si el servidor SMTP falla momentáneamente
        }
    }

    public async Task SendSaleReceiptAsync(SaleDto sale, string customerEmail, CancellationToken cancellationToken = default)
    {
        var subject = $"Comprobante Oficial de Compra - Firmeza S.A.S. (Orden #{sale.Id.ToString()[..8].ToUpper()})";
        
        var itemsHtml = new StringBuilder();
        foreach (var item in sale.Details)
        {
            itemsHtml.Append($@"
                <tr style='border-bottom: 1px solid #e2e8f0;'>
                    <td style='padding: 10px 12px; font-size: 13px; color: #1e293b;'>{item.ProductName}</td>
                    <td style='padding: 10px 12px; font-size: 13px; color: #475569; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 10px 12px; font-size: 13px; color: #475569; text-align: right;'>${item.UnitPrice:N0} {item.Currency}</td>
                    <td style='padding: 10px 12px; font-size: 13px; font-weight: bold; color: #0f172a; text-align: right;'>${item.Subtotal:N0} {item.Currency}</td>
                </tr>");
        }

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Comprobante de Compra - Firmeza</title>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 24px; color: #0f172a;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.05);'>
        
        <!-- Header -->
        <div style='background-color: #0f172a; padding: 24px; text-align: center;'>
            <h1 style='color: #f59e0b; margin: 0; font-size: 24px; letter-spacing: 1px; text-transform: uppercase;'>FIRMEZA</h1>
            <p style='color: #94a3b8; margin: 4px 0 0 0; font-size: 12px;'>Materiales de Construcción & Alquiler de Maquinaria</p>
        </div>

        <!-- Body -->
        <div style='padding: 24px;'>
            <div style='background-color: #ecfdf5; border-left: 4px solid #10b981; padding: 12px 16px; border-radius: 4px; margin-bottom: 20px;'>
                <p style='margin: 0; font-weight: bold; color: #065f46; font-size: 14px;'>¡Gracias por tu compra, {sale.CustomerName}!</p>
                <p style='margin: 4px 0 0 0; color: #047857; font-size: 12px;'>Tu pedido ha sido procesado exitosamente y tus materiales están listos para despacho.</p>
            </div>

            <table style='width: 100%; font-size: 13px; margin-bottom: 20px;'>
                <tr>
                    <td style='color: #64748b;'><strong>Número de Orden:</strong></td>
                    <td style='text-align: right; font-family: monospace; color: #0f172a;'>{sale.Id}</td>
                </tr>
                <tr>
                    <td style='color: #64748b;'><strong>Fecha:</strong></td>
                    <td style='text-align: right; color: #0f172a;'>{sale.SaleDate:dd/MM/yyyy HH:mm}</td>
                </tr>
                <tr>
                    <td style='color: #64748b;'><strong>Estado:</strong></td>
                    <td style='text-align: right; color: #10b981; font-weight: bold;'>{sale.Status}</td>
                </tr>
            </table>

            <!-- Tabla de Productos -->
            <table style='width: 100%; border-collapse: collapse; margin-top: 16px;'>
                <thead>
                    <tr style='background-color: #f1f5f9; text-align: left; font-size: 12px; color: #475569; text-transform: uppercase;'>
                        <th style='padding: 10px 12px;'>Ítem</th>
                        <th style='padding: 10px 12px; text-align: center;'>Cant.</th>
                        <th style='padding: 10px 12px; text-align: right;'>Precio</th>
                        <th style='padding: 10px 12px; text-align: right;'>Subtotal</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsHtml}
                </tbody>
            </table>

            <!-- Resumen Total -->
            <div style='margin-top: 20px; text-align: right; padding-top: 16px; border-top: 2px solid #e2e8f0;'>
                <p style='margin: 4px 0; font-size: 18px; font-weight: 800; color: #0f172a;'>
                    Total Liquidado: <span style='color: #d97706;'>${sale.TotalAmount:N0} {sale.Currency}</span>
                </p>
                <p style='margin: 0; font-size: 11px; color: #64748b;'>(Incluye IVA 19% e impuestos legales)</p>
            </div>

            <!-- Información adicional -->
            <div style='margin-top: 28px; padding: 14px; background-color: #f8fafc; border-radius: 8px; font-size: 11px; color: #64748b;'>
                <p style='margin: 0 0 4px 0;'><strong>Soporte y Despacho:</strong> Carrera 45 # 120-30, Bogotá D.C. | Tel: +57 (601) 789-0123</p>
                <p style='margin: 0;'>Este es un comprobante digital generado automáticamente por la plataforma Firmeza.</p>
            </div>
        </div>

        <!-- Footer -->
        <div style='background-color: #f1f5f9; padding: 14px; text-align: center; font-size: 11px; color: #94a3b8;'>
            © {DateTime.UtcNow.Year} Firmeza S.A.S. Todos los derechos reservados.
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(customerEmail, subject, body, true, cancellationToken);
    }

    public async Task SendWelcomeEmailAsync(string customerEmail, string customerName, CancellationToken cancellationToken = default)
    {
        var subject = "¡Bienvenido a Firmeza! - Cuenta de Cliente Activada";
        var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /></head>
<body style='font-family: sans-serif; background-color: #f8fafc; padding: 20px;'>
    <div style='max-width: 500px; margin: 0 auto; background: white; padding: 24px; border-radius: 8px; border: 1px solid #e2e8f0;'>
        <h2 style='color: #f59e0b; margin-top: 0;'>FIRMEZA</h2>
        <h3>Hola, {customerName}</h3>
        <p>Tu cuenta como cliente de Firmeza ha sido creada exitosamente. Ya puedes ingresar a nuestra plataforma para explorar el catálogo de materiales y maquinaria pesada, gestionar tus pedidos y recibir tus comprobantes directamente en este correo.</p>
        <p style='color: #64748b; font-size: 12px; margin-top: 20px;'>Firmeza S.A.S. - Construcción y Maquinaria sin intermediarios.</p>
    </div>
</body>
</html>";

        await SendEmailAsync(customerEmail, subject, body, true, cancellationToken);
    }
}
