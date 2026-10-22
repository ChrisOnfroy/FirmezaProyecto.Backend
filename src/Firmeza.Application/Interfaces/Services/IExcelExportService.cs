using Firmeza.Application.DTOs.Products;

namespace Firmeza.Application.Interfaces.Services;

public interface IExcelExportService
{
    Task<byte[]> ExportProductsToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default);
}
