using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly IPdfExportService _pdfExportService;

    public SalesController(ISaleService saleService, IPdfExportService pdfExportService)
    {
        _saleService = saleService;
        _pdfExportService = pdfExportService;
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportSalesReportToPdf([FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        var sales = customerId.HasValue
            ? await _saleService.GetByCustomerIdAsync(customerId.Value, cancellationToken)
            : await _saleService.GetAllAsync(cancellationToken);

        var pdfBytes = await _pdfExportService.ExportSalesReportToPdfAsync(sales, cancellationToken);
        var fileName = $"Reporte_Ventas_Firmeza_{DateTime.Now:yyyyMMdd_HHmm}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportSaleInvoiceToPdf(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _saleService.GetByIdAsync(id, cancellationToken);
        if (sale is null)
            return NotFound(new { message = $"Venta con Id '{id}' no encontrada." });

        var pdfBytes = await _pdfExportService.ExportSaleInvoiceToPdfAsync(sale, cancellationToken);
        var fileName = $"Factura_Venta_FZ-{sale.Id.ToString()[..8].ToUpperInvariant()}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        var sales = customerId.HasValue
            ? await _saleService.GetByCustomerIdAsync(customerId.Value, cancellationToken)
            : await _saleService.GetAllAsync(cancellationToken);

        return Ok(sales);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _saleService.GetByIdAsync(id, cancellationToken);
        if (sale is null)
            return NotFound(new { message = $"Venta con Id '{id}' no encontrada." });

        return Ok(sale);
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> DownloadReceipt(Guid id, CancellationToken cancellationToken)
    {
        var sale = await _saleService.GetByIdAsync(id, cancellationToken);
        if (sale is null)
            return NotFound(new { message = $"Venta con Id '{id}' no encontrada." });

        var itemsRows = string.Join("", sale.Details.Select(d =>
            $"<tr><td style='padding:8px;border-bottom:1px solid #e2e8f0;'>{d.ProductName}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:center;'>{d.Quantity}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:right;'>${d.UnitPrice:N0}</td>" +
            $"<td style='padding:8px;border-bottom:1px solid #e2e8f0;text-align:right;font-weight:bold;'>${d.Subtotal:N0} {d.Currency}</td></tr>"));

        var receiptHtml = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Comprobante Firmeza #{sale.Id}</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, sans-serif; padding: 40px; color: #0f172a; }}
        .header {{ border-bottom: 2px solid #0f172a; padding-bottom: 12px; margin-bottom: 24px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th {{ background: #f8fafc; padding: 8px; text-align: left; font-size: 12px; text-transform: uppercase; }}
        .total-box {{ text-align: right; margin-top: 24px; padding-top: 12px; border-top: 2px solid #0f172a; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1 style='margin:0;color:#f59e0b;'>FIRMEZA S.A.S.</h1>
        <p style='margin:4px 0 0 0;font-size:12px;color:#64748b;'>Materiales de Construcción & Alquiler de Maquinaria - NIT: 901.458.789-1</p>
    </div>
    <p><strong>Comprobante de Venta / Factura:</strong> {sale.Id}</p>
    <p><strong>Cliente:</strong> {sale.CustomerName}</p>
    <p><strong>Fecha:</strong> {sale.SaleDate:dd/MM/yyyy HH:mm}</p>
    <p><strong>Estado:</strong> {sale.Status}</p>
    <table>
        <thead><tr><th>Producto</th><th style='text-align:center;'>Cant.</th><th style='text-align:right;'>Precio Unitario</th><th style='text-align:right;'>Subtotal</th></tr></thead>
        <tbody>{itemsRows}</tbody>
    </table>
    <div class='total-box'>
        <h2 style='margin:0;color:#0f172a;'>Total Pagado: ${sale.TotalAmount:N0} {sale.Currency}</h2>
        <p style='margin:4px 0 0 0;font-size:11px;color:#64748b;'>Incluye IVA (19%) e impuestos de ley</p>
    </div>
</body>
</html>";

        var bytes = System.Text.Encoding.UTF8.GetBytes(receiptHtml);
        return File(bytes, "text/html", $"comprobante-firmeza-{sale.Id}.html");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var sale = await _saleService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var cancelled = await _saleService.CancelAsync(id, cancellationToken);
            if (!cancelled)
                return NotFound(new { message = $"Venta con Id '{id}' no encontrada." });

            return Ok(new { message = "Venta cancelada exitosamente y stock restablecido." });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
