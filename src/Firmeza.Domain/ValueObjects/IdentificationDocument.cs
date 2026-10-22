using Firmeza.Domain.Common;
using Firmeza.Domain.Enums;

namespace Firmeza.Domain.ValueObjects;

public class IdentificationDocument : ValueObject
{
    public IdentificationType Type { get; }
    public string Number { get; }

    protected IdentificationDocument()
    {
        Type = IdentificationType.NationalIdentity;
        Number = string.Empty;
    }

    public IdentificationDocument(IdentificationType type, string number)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("El número de documento no puede estar vacío.", nameof(number));

        Type = type;
        Number = number.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Number;
    }

    public override string ToString() => $"{Type}: {Number}";
}
