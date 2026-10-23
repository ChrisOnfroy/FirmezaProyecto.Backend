using Firmeza.Application.DTOs.Products;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace Firmeza.UnitTests.Services;

public class ExportServicesTests
{
    [Fact]
    public async Task ExportPriceListToExcelAsync_WithValidProducts_ReturnsNonEmptyExcelByteArray()
    {
        // Arrange
        var service = new ExcelExportService();
        var sampleProducts = new List<ProductDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Cemento Gris Uso General Argos 50 Kg",
                CategoryName = "Cemento y Mampostería",
                Description = "Cemento Portland de alta resistencia",
                Price = 34500m,
                Currency = "COP",
                Stock = 200,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Varilla Corrugada 1/2 pulgada x 6 m",
                CategoryName = "Acero y Estructuras",
                Description = "Acero de refuerzo grado 60",
                Price = 38900m,
                Currency = "COP",
                Stock = 0,
                IsActive = true
            }
        };

        // Act
        var result = await service.ExportPriceListToExcelAsync(sampleProducts);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(1000); // Archivo XLSX válido con compresión zip
    }

    [Fact]
    public async Task ExportInventoryReportToExcelAsync_WithValidProducts_ReturnsNonEmptyExcelByteArray()
    {
        // Arrange
        var service = new ExcelExportService();
        var sampleProducts = new List<ProductDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Cemento Gris Uso General Argos 50 Kg",
                CategoryName = "Cemento y Mampostería",
                Description = "Cemento Portland de alta resistencia",
                Price = 34500m,
                Currency = "COP",
                Stock = 200,
                IsActive = true
            }
        };

        // Act
        var result = await service.ExportInventoryReportToExcelAsync(sampleProducts);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task ExportProductsToExcelAsync_WithValidProducts_ReturnsNonEmptyExcelByteArray()
    {
        // Arrange
        var service = new ExcelExportService();
        var sampleProducts = new List<ProductDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Cemento Gris Uso General Argos 50 Kg",
                CategoryName = "Cemento y Mampostería",
                Description = "Cemento Portland de alta resistencia",
                Price = 34500m,
                Currency = "COP",
                Stock = 200,
                IsActive = true
            }
        };

        // Act
        var result = await service.ExportProductsToExcelAsync(sampleProducts);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public async Task ExportSalesReportToPdfAsync_WithValidSales_ReturnsNonEmptyPdfByteArray()
    {
        // Arrange
        var service = new PdfExportService();
        var sampleSales = new List<SaleDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                CustomerName = "Constructora El Roble S.A.S.",
                CustomerIdentification = "NIT 900765432-1",
                CustomerEmail = "compras@constructoraelroble.com",
                CustomerPhone = "+57 3201112233",
                SaleDate = DateTime.UtcNow.AddDays(-2),
                TotalAmount = 4042000m,
                Currency = "COP",
                Status = "Completed",
                Details = new List<SaleDetailDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductName = "Cemento Gris Argos 50 Kg",
                        Quantity = 50,
                        UnitPrice = 34500m,
                        Subtotal = 1725000m,
                        Currency = "COP"
                    }
                }
            }
        };

        // Act
        var result = await service.ExportSalesReportToPdfAsync(sampleSales);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(500);
        result.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }

    [Fact]
    public async Task ExportSaleInvoiceToPdfAsync_WithValidSale_ReturnsValidInvoicePdf()
    {
        // Arrange
        var service = new PdfExportService();
        var sampleSale = new SaleDto
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Laura Morales Sánchez",
            CustomerIdentification = "C.C. 1020304050",
            CustomerEmail = "cliente@firmeza.com",
            CustomerPhone = "+57 3154567890",
            CustomerAddress = "Carrera 15 # 85-32 Apto 402, Bogotá",
            SaleDate = DateTime.UtcNow,
            TotalAmount = 577000m,
            Currency = "COP",
            Status = "Pending",
            Details = new List<SaleDetailDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Pintura Viniltex Tipo 1 Blanco Balde 5 Galones",
                    Quantity = 2,
                    UnitPrice = 168000m,
                    Subtotal = 336000m,
                    Currency = "COP"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Estuco Plástico Listo Corona Balde 25 Kg",
                    Quantity = 3,
                    UnitPrice = 52000m,
                    Subtotal = 156000m,
                    Currency = "COP"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Impermeabilizante Sika 1 Galón 4 Kg",
                    Quantity = 2,
                    UnitPrice = 42500m,
                    Subtotal = 85000m,
                    Currency = "COP"
                }
            }
        };

        // Act
        var result = await service.ExportSaleInvoiceToPdfAsync(sampleSale);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(500);
        result.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }
}
