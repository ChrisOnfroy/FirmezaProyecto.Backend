using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Repositories;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Application.Services;
using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace Firmeza.UnitTests.Application;

public class SaleServiceTests
{
    private readonly Mock<ISaleRepository> _saleRepoMock;
    private readonly Mock<ICustomerRepository> _customerRepoMock;
    private readonly Mock<IProductRepository> _productRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly SaleService _saleService;

    public SaleServiceTests()
    {
        _saleRepoMock = new Mock<ISaleRepository>();
        _customerRepoMock = new Mock<ICustomerRepository>();
        _productRepoMock = new Mock<IProductRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _emailServiceMock = new Mock<IEmailService>();

        _saleService = new SaleService(
            _saleRepoMock.Object,
            _customerRepoMock.Object,
            _productRepoMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenValidRequest_CreatesSaleReducesStockAndSendsEmailReceipt()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var customer = new Customer(
            "Juan",
            "Pérez",
            new IdentificationDocument(IdentificationType.NationalIdentity, "12345678"),
            new Email("juan.perez@firmeza.com"));

        var product = new Product(
            "Bolsa Cemento Gris",
            new Money(32000m, "COP"),
            stock: 20,
            categoryId: Guid.NewGuid());

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepoMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new CreateSaleDto
        {
            CustomerId = customerId,
            Currency = "COP",
            Details = new List<CreateSaleDetailDto>
            {
                new() { ProductId = productId, Quantity = 5 }
            }
        };

        // Act
        var result = await _saleService.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customerId);
        result.TotalAmount.Should().Be(160000m); // 5 * 32000
        product.Stock.Should().Be(15); // 20 - 5

        _saleRepoMock.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verifica que se envió el comprobante por correo SMTP (TASK 5 & 7)
        _emailServiceMock.Verify(
            e => e.SendSaleReceiptAsync(It.IsAny<SaleDto>(), "juan.perez@firmeza.com", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerNotFound_ThrowsDomainException()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var request = new CreateSaleDto
        {
            CustomerId = customerId,
            Currency = "COP",
            Details = new List<CreateSaleDetailDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        // Act
        Func<Task> act = async () => await _saleService.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*no existe*");
    }

    [Fact]
    public async Task CreateAsync_WhenCustomerInactive_ThrowsDomainException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var customer = new Customer(
            "Maria",
            "Lopez",
            new IdentificationDocument(IdentificationType.NationalIdentity, "87654321"),
            new Email("maria@firmeza.com"));
        customer.Deactivate();

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var request = new CreateSaleDto
        {
            CustomerId = customerId,
            Currency = "COP",
            Details = new List<CreateSaleDetailDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        // Act
        Func<Task> act = async () => await _saleService.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*está inactivo*");
    }

    [Fact]
    public async Task CreateAsync_WhenStockIsInsufficient_ThrowsInsufficientStockException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var customer = new Customer(
            "Andrés",
            "Castro",
            new IdentificationDocument(IdentificationType.NationalIdentity, "99887766"),
            new Email("andres@firmeza.com"));

        var product = new Product(
            "Arena Fina M3",
            new Money(50000m, "COP"),
            stock: 2,
            categoryId: Guid.NewGuid());

        _customerRepoMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _productRepoMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var request = new CreateSaleDto
        {
            CustomerId = customerId,
            Currency = "COP",
            Details = new List<CreateSaleDetailDto>
            {
                new() { ProductId = productId, Quantity = 5 } // Stock es 2, pide 5
            }
        };

        // Act
        Func<Task> act = async () => await _saleService.CreateAsync(request);

        // Assert
        await act.Should().ThrowAsync<InsufficientStockException>();
        product.Stock.Should().Be(2); // No debe alterarse
    }
}
