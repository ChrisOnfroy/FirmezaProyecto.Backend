using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Interfaces.Services;

public interface IPdfExportService
{
    Task<byte[]> ExportSalesReportToPdfAsync(IReadOnlyList<SaleDto> sales, CancellationToken cancellationToken = default);
    Task<byte[]> ExportSaleInvoiceToPdfAsync(SaleDto sale, CancellationToken cancellationToken = default);
}
