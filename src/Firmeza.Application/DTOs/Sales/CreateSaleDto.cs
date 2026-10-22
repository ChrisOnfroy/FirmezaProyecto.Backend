namespace Firmeza.Application.DTOs.Sales;

public class CreateSaleDetailDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CreateSaleDto
{
    public Guid CustomerId { get; set; }
    public string Currency { get; set; } = "COP";
    public List<CreateSaleDetailDto> Details { get; set; } = new();
}
