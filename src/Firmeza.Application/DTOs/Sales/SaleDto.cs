namespace Firmeza.Application.DTOs.Sales;

public class SaleDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string? CustomerIdentification { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "COP";
    public string Status { get; set; } = string.Empty;
    public IReadOnlyList<SaleDetailDto> Details { get; set; } = Array.Empty<SaleDetailDto>();
}
