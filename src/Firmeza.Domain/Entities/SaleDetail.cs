using Firmeza.Domain.Common;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;

namespace Firmeza.Domain.Entities;

public class SaleDetail : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = null!;
    public Money Subtotal => UnitPrice * Quantity;

    protected SaleDetail() { }

    public SaleDetail(Guid productId, int quantity, Money unitPrice)
    {
        if (quantity <= 0)
            throw new DomainException("La cantidad vendida debe ser mayor a cero.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
