using System.Text.RegularExpressions;
using Firmeza.Domain.Common;

namespace Firmeza.Domain.ValueObjects;

public class PhoneNumber : ValueObject
{
    private static readonly Regex PhoneRegex = new(@"^\+?[0-9]{7,15}$", RegexOptions.Compiled);

    public string CountryCode { get; }
    public string Number { get; }

    protected PhoneNumber()
    {
        CountryCode = string.Empty;
        Number = string.Empty;
    }

    public PhoneNumber(string countryCode, string number)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            throw new ArgumentException("El código de país es requerido.", nameof(countryCode));

        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("El número de teléfono es requerido.", nameof(number));

        countryCode = countryCode.Trim();
        number = number.Trim();

        var fullPhone = countryCode.StartsWith('+') ? $"{countryCode}{number}" : $"+{countryCode}{number}";
        if (!PhoneRegex.IsMatch(fullPhone))
            throw new ArgumentException($"El número de teléfono '{fullPhone}' no es válido.", nameof(number));

        CountryCode = countryCode;
        Number = number;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CountryCode;
        yield return Number;
    }

    public override string ToString() => $"{CountryCode} {Number}";
}
