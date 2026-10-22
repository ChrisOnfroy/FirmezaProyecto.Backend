using Firmeza.Domain.Common;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;

namespace Firmeza.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Money Price { get; set; } = null!;
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    protected Product() { }

    public Product(string name, Money price, int stock, Guid categoryId, string? description = null)
    {
        Name = name;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        Description = description;
    }

    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("La cantidad a descontar debe ser mayor a cero.");

        if (quantity > Stock)
            throw new InsufficientStockException(Name, Stock, quantity);

        Stock -= quantity;
        SetUpdatedAt();
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("La cantidad a reabastecer debe ser mayor a cero.");

        Stock += quantity;
        SetUpdatedAt();
    }
}
