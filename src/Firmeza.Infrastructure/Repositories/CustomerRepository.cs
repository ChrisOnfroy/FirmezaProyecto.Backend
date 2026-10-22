using Firmeza.Application.Interfaces.Repositories;
using Firmeza.Domain.Entities;
using Firmeza.Domain.ValueObjects;
using Firmeza.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Firmeza.Infrastructure.Repositories;

public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByIdentificationAsync(string identificationNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Identification.Number == identificationNumber.Trim(), cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailVo = new Email(email);
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Email == emailVo, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailVo = new Email(email);
        return await _dbSet
            .AnyAsync(c => c.Email == emailVo, cancellationToken);
    }
}
