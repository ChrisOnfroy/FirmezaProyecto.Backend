using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.Interfaces.Repositories;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;

namespace Firmeza.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto?> GetByIdentificationAsync(string number, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdentificationAsync(number, cancellationToken);
        return customer is null ? null : MapToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        if (await _customerRepository.ExistsByEmailAsync(dto.Email, cancellationToken))
            throw new DomainException($"Ya existe un cliente con el correo electrónico '{dto.Email}'.");

        var existingById = await _customerRepository.GetByIdentificationAsync(dto.IdentificationNumber, cancellationToken);
        if (existingById is not null)
            throw new DomainException($"Ya existe un cliente con el documento '{dto.IdentificationNumber}'.");

        var identificationType = Enum.IsDefined(typeof(IdentificationType), dto.IdentificationType)
            ? (IdentificationType)dto.IdentificationType
            : IdentificationType.NationalIdentity;

        var identification = new IdentificationDocument(identificationType, dto.IdentificationNumber);
        var email = new Email(dto.Email);

        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
        {
            var countryCode = !string.IsNullOrWhiteSpace(dto.CountryCode) ? dto.CountryCode : "+57";
            phoneNumber = new PhoneNumber(countryCode, dto.PhoneNumber);
        }

        Address? address = null;
        if (!string.IsNullOrWhiteSpace(dto.Street) && !string.IsNullOrWhiteSpace(dto.City) && !string.IsNullOrWhiteSpace(dto.Country))
        {
            address = new Address(
                dto.Street,
                dto.City,
                dto.State ?? string.Empty,
                dto.PostalCode ?? string.Empty,
                dto.Country);
        }

        var customer = new Customer(
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            identification,
            email,
            phoneNumber,
            address);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null) return false;

        customer.Deactivate();
        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static CustomerDto MapToDto(Customer customer) => new()
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        IdentificationType = customer.Identification.Type.ToString(),
        IdentificationNumber = customer.Identification.Number,
        Email = customer.Email.Value,
        PhoneNumber = customer.PhoneNumber?.ToString(),
        Address = customer.Address?.ToString(),
        IsActive = customer.IsActive,
        CreatedAt = customer.CreatedAt
    };
}
