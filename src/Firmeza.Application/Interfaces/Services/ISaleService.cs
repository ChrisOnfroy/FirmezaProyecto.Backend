using Firmeza.Application.DTOs.Sales;

namespace Firmeza.Application.Interfaces.Services;

public interface ISaleService
{
    Task<IReadOnlyList<SaleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaleDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<SaleDto> CreateAsync(CreateSaleDto dto, CancellationToken cancellationToken = default);
    Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
