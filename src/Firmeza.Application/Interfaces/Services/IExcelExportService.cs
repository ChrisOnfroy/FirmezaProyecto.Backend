using Firmeza.Application.DTOs.Products;

namespace Firmeza.Application.Interfaces.Services;

public interface IExcelExportService
{
    /// <summary>
    /// Genera la lista oficial de precios para clientes en formato Excel (solo productos y precios, sin revelar stock ni valor de inventario).
    /// </summary>
    Task<byte[]> ExportPriceListToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera el reporte de inventario interno administrativo con existencias y valorización para gestión de la empresa.
    /// </summary>
    Task<byte[]> ExportInventoryReportToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default);

    /// <summary>
    /// Método por compatibilidad que genera la lista oficial de precios para clientes.
    /// </summary>
    Task<byte[]> ExportProductsToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default);
}
