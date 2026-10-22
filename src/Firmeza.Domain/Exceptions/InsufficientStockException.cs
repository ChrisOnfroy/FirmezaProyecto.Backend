namespace Firmeza.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public InsufficientStockException(string productName, int currentStock, int requestedQuantity)
        : base($"Stock insuficiente para el producto '{productName}'. Stock actual: {currentStock}, solicitado: {requestedQuantity}.")
    {
    }
}
