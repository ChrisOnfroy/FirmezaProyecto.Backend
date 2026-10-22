namespace Firmeza.Application.DTOs.Products;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "COP";
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
}
