using Firmeza.Domain.Common;
using Firmeza.Domain.ValueObjects;

namespace Firmeza.Domain.Entities;

public class Customer : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public IdentificationDocument Identification { get; set; } = null!;
    public Email Email { get; set; } = null!;
    public PhoneNumber? PhoneNumber { get; set; }
    public Address? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Sale> Sales { get; set; } = new List<Sale>();

    protected Customer() { }

    public Customer(
        string firstName,
        string lastName,
        IdentificationDocument identification,
        Email email,
        PhoneNumber? phoneNumber = null,
        Address? address = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Identification = identification;
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}
