using Firmeza.Domain.Common;
using Firmeza.Domain.Enums;
using Firmeza.Domain.Exceptions;
using Firmeza.Domain.ValueObjects;

namespace Firmeza.Domain.Entities;

public class Sale : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public Money TotalAmount { get; set; } = Money.Zero();
    public SaleStatus Status { get; set; } = SaleStatus.Pending;

    public ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();

    protected Sale() { }

    public Sale(Guid customerId, string currency = "COP")
    {
        CustomerId = customerId;
        TotalAmount = Money.Zero(currency);
        Status = SaleStatus.Pending;
        SaleDate = DateTime.UtcNow;
    }

    public void AddDetail(Product product, int quantity)
    {
        if (Status != SaleStatus.Pending)
            throw new DomainException("No se pueden agregar productos a una venta que no esté en estado pendiente.");

        product.DeductStock(quantity);

        var detail = new SaleDetail(product.Id, quantity, product.Price);
        SaleDetails.Add(detail);

        TotalAmount += detail.Subtotal;
        SetUpdatedAt();
    }

    public void Complete()
    {
        if (Status != SaleStatus.Pending)
            throw new DomainException("Solo se pueden completar ventas que estén en estado pendiente.");

        if (SaleDetails.Count == 0)
            throw new DomainException("No se puede completar una venta sin detalles de productos.");

        Status = SaleStatus.Completed;
        SetUpdatedAt();
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Completed)
            throw new DomainException("No se puede cancelar una venta que ya ha sido completada.");

        Status = SaleStatus.Cancelled;
        SetUpdatedAt();
    }
}
