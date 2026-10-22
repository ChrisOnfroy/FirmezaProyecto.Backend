using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Repositories;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Exceptions;

namespace Firmeza.Application.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public SaleService(
        ISaleRepository saleRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService)
    {
        _saleRepository = saleRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<IReadOnlyList<SaleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.GetAllAsync(cancellationToken);
        return sales.Select(MapToDto).ToList();
    }

    public async Task<SaleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetWithDetailsByIdAsync(id, cancellationToken);
        return sale is null ? null : MapToDto(sale);
    }

    public async Task<IReadOnlyList<SaleDto>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var sales = await _saleRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        return sales.Select(MapToDto).ToList();
    }

    public async Task<SaleDto> CreateAsync(CreateSaleDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.CustomerId, cancellationToken);
        if (customer is null)
            throw new DomainException($"El cliente con Id '{dto.CustomerId}' no existe.");

        if (!customer.IsActive)
            throw new DomainException("El cliente está inactivo y no puede realizar compras.");

        if (dto.Details is null || dto.Details.Count == 0)
            throw new DomainException("La venta debe contener al menos un producto.");

        var sale = new Sale(dto.CustomerId, dto.Currency);

        foreach (var detailDto in dto.Details)
        {
            var product = await _productRepository.GetByIdAsync(detailDto.ProductId, cancellationToken);
            if (product is null)
                throw new DomainException($"El producto con Id '{detailDto.ProductId}' no existe.");

            if (!product.IsActive)
                throw new DomainException($"El producto '{product.Name}' está inactivo.");

            // AddDetail will deduct stock from product and recalculate total
            sale.AddDetail(product, detailDto.Quantity);
            _productRepository.Update(product);
        }

        sale.Complete();

        await _saleRepository.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var resultDto = MapToDto(sale);

        // Envío de comprobante por correo electrónico SMTP (TASK 5 & 7)
        try
        {
            if (!string.IsNullOrWhiteSpace(customer.Email?.Value))
            {
                await _emailService.SendSaleReceiptAsync(resultDto, customer.Email.Value, cancellationToken);
            }
        }
        catch
        {
            // El fallo temporal de red SMTP no interrumpe la persistencia de la venta
        }

        return resultDto;
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _saleRepository.GetWithDetailsByIdAsync(id, cancellationToken);
        if (sale is null) return false;

        sale.Cancel();

        // Restock products upon cancellation
        foreach (var detail in sale.SaleDetails)
        {
            var product = await _productRepository.GetByIdAsync(detail.ProductId, cancellationToken);
            if (product is not null)
            {
                product.AddStock(detail.Quantity);
                _productRepository.Update(product);
            }
        }

        _saleRepository.Update(sale);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static SaleDto MapToDto(Sale sale) => new()
    {
        Id = sale.Id,
        CustomerId = sale.CustomerId,
        CustomerName = sale.Customer != null ? $"{sale.Customer.FirstName} {sale.Customer.LastName}" : string.Empty,
        SaleDate = sale.SaleDate,
        TotalAmount = sale.TotalAmount.Amount,
        Currency = sale.TotalAmount.Currency,
        Status = sale.Status.ToString(),
        Details = sale.SaleDetails.Select(d => new SaleDetailDto
        {
            Id = d.Id,
            ProductId = d.ProductId,
            ProductName = d.Product?.Name ?? string.Empty,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice.Amount,
            Subtotal = d.Subtotal.Amount,
            Currency = d.UnitPrice.Currency
        }).ToList()
    };
}
