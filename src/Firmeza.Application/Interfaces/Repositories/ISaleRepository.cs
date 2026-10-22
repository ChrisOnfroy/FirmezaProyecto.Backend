using Firmeza.Domain.Entities;

namespace Firmeza.Application.Interfaces.Repositories;

public interface ISaleRepository : IBaseRepository<Sale>
{
    Task<IReadOnlyList<Sale>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
    Task<Sale?> GetWithDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Sale>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
}
