using Firmeza.Domain.Entities;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Firmeza.UnitTests.Domain;

public class SaleTests
{
    [Fact]
    public void Sale_Constructor_InitializesWithPendingStatusAndZeroTotal()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        // Act
        var sale = new Sale(customerId, "COP");

        // Assert
        sale.CustomerId.Should().Be(customerId);
        sale.Status.Should().Be(SaleStatus.Pending);
        sale.TotalAmount.Amount.Should().Be(0);
        sale.TotalAmount.Currency.Should().Be("COP");
        sale.SaleDetails.Should().BeEmpty();
    }

    [Fact]
    public void Sale_AddDetail_DeductsProductStockAndIncreasesTotal()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var sale = new Sale(customerId, "COP");
        var product = new Product("Cemento Gris", new Money(30000m, "COP"), 100, Guid.NewGuid());

        // Act
        sale.AddDetail(product, 3); // 3 * 30000 = 90000

        // Assert
        product.Stock.Should().Be(97);
        sale.SaleDetails.Should().HaveCount(1);
        sale.TotalAmount.Amount.Should().Be(90000m);
    }

    [Fact]
    public void Sale_Complete_WhenValidDetailsExist_SetsStatusToCompleted()
    {
        // Arrange
        var sale = new Sale(Guid.NewGuid(), "COP");
        var product = new Product("Teja Ondulada", new Money(25000m, "COP"), 50, Guid.NewGuid());
        sale.AddDetail(product, 2);

        // Act
        sale.Complete();

        // Assert
        sale.Status.Should().Be(SaleStatus.Completed);
    }

    [Fact]
    public void Sale_Complete_WithoutDetails_ThrowsDomainException()
    {
        // Arrange
        var sale = new Sale(Guid.NewGuid(), "COP");

        // Act
        Action act = () => sale.Complete();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*sin detalles de productos*");
    }

    [Fact]
    public void Sale_Cancel_WhenAlreadyCompleted_ThrowsDomainException()
    {
        // Arrange
        var sale = new Sale(Guid.NewGuid(), "COP");
        var product = new Product("Yeso", new Money(15000m, "COP"), 20, Guid.NewGuid());
        sale.AddDetail(product, 1);
        sale.Complete();

        // Act
        Action act = () => sale.Cancel();

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*ya ha sido completada*");
    }
}
