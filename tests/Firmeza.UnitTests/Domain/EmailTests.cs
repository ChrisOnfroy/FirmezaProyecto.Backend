using Firmeza.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Firmeza.UnitTests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("cliente@firmeza.com")]
    [InlineData("usuario.prueba@empresa.com.co")]
    [InlineData("ventas+obras@constructora.net")]
    public void Email_Constructor_WithValidEmail_CreatesAndNormalizes(string input)
    {
        // Act
        var email = new Email(input);

        // Assert
        email.Value.Should().Be(input.ToLowerInvariant());
        email.ToString().Should().Be(input.ToLowerInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Email_Constructor_WithEmptyOrNull_ThrowsArgumentException(string? input)
    {
        // Act
        Action act = () => new Email(input!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("correo-invalido")]
    [InlineData("@dominio.com")]
    [InlineData("cliente@")]
    [InlineData("cliente@dominio")]
    public void Email_Constructor_WithInvalidFormat_ThrowsArgumentException(string invalidInput)
    {
        // Act
        Action act = () => new Email(invalidInput);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*no es válido*");
    }

    [Fact]
    public void Email_Equality_IsCaseInsensitive()
    {
        // Arrange
        var email1 = new Email("Cliente@Firmeza.COM");
        var email2 = new Email("cliente@firmeza.com");

        // Assert
        email1.Should().Be(email2);
    }
}
