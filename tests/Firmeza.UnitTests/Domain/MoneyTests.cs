using Firmeza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Firmeza.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Money_Constructor_WithValidAmount_CreatesInstance()
    {
        // Act
        var money = new Money(50000m, "COP");

        // Assert
        money.Amount.Should().Be(50000m);
        money.Currency.Should().Be("COP");
    }

    [Fact]
    public void Money_Constructor_WithNegativeAmount_ThrowsArgumentException()
    {
        // Act
        Action act = () => new Money(-10m, "COP");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*no puede ser negativo*");
    }

    [Fact]
    public void Money_Addition_SameCurrency_CalculatesCorrectSum()
    {
        // Arrange
        var m1 = new Money(30000m, "COP");
        var m2 = new Money(25000m, "COP");

        // Act
        var result = m1 + m2;

        // Assert
        result.Amount.Should().Be(55000m);
        result.Currency.Should().Be("COP");
    }

    [Fact]
    public void Money_Addition_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var cop = new Money(1000m, "COP");
        var usd = new Money(10m, "USD");

        // Act
        Action act = () => { var _ = cop + usd; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*monedas distintas*");
    }

    [Fact]
    public void Money_Multiplication_CalculatesCorrectProduct()
    {
        // Arrange
        var money = new Money(15000m, "COP");

        // Act
        var result = money * 3;

        // Assert
        result.Amount.Should().Be(45000m);
        result.Currency.Should().Be("COP");
    }

    [Fact]
    public void Money_Equality_SameAmountAndCurrency_AreEqual()
    {
        // Arrange
        var m1 = new Money(20000m, "cop");
        var m2 = new Money(20000m, "COP");

        // Assert
        m1.Should().Be(m2);
        (m1 == m2).Should().BeTrue();
    }
}
