using Firmeza.Domain.Entities;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Firmeza.UnitTests.Domain;

public class ProductTests
{
    [Fact]
    public void DeductStock_WithValidQuantity_ReducesStockCorrectly()
    {
        // Arrange
        var product = new Product(
            name: "Cemento Portland",
            price: new Money(35000m, "COP"),
            stock: 50,
            categoryId: Guid.NewGuid());

        // Act
        product.DeductStock(10);

        // Assert
        product.Stock.Should().Be(40);
    }

    [Fact]
    public void DeductStock_WhenQuantityExceedsStock_ThrowsInsufficientStockException()
    {
        // Arrange
        var product = new Product(
            name: "Arena Fina M3",
            price: new Money(80000m, "COP"),
            stock: 5,
            categoryId: Guid.NewGuid());

        // Act
        Action act = () => product.DeductStock(10);

        // Assert
        act.Should().Throw<InsufficientStockException>()
            .WithMessage("*Stock insuficiente*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DeductStock_WithZeroOrNegativeQuantity_ThrowsDomainException(int invalidQty)
    {
        // Arrange
        var product = new Product(
            name: "Ladrillo Estructural",
            price: new Money(1200m, "COP"),
            stock: 100,
            categoryId: Guid.NewGuid());

        // Act
        Action act = () => product.DeductStock(invalidQty);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*mayor a cero*");
    }

    [Fact]
    public void AddStock_WithValidQuantity_IncreasesStockCorrectly()
    {
        // Arrange
        var product = new Product(
            name: "Varilla Corrugada",
            price: new Money(45000m, "COP"),
            stock: 20,
            categoryId: Guid.NewGuid());

        // Act
        product.AddStock(15);

        // Assert
        product.Stock.Should().Be(35);
    }
}
