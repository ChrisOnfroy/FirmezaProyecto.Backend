using Firmeza.Application.DTOs.Products;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IExcelExportService _excelExportService;

    public ProductsController(IProductService productService, IExcelExportService excelExportService)
    {
        _productService = productService;
        _excelExportService = excelExportService;
    }

    [HttpGet("export/excel")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportToExcel([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var products = categoryId.HasValue
            ? await _productService.GetByCategoryIdAsync(categoryId.Value, cancellationToken)
            : await _productService.GetAllAsync(cancellationToken);

        var fileBytes = await _excelExportService.ExportProductsToExcelAsync(products, cancellationToken);
        var fileName = $"Catalogo_Productos_Firmeza_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] Guid? categoryId, CancellationToken cancellationToken)
    {
        var products = categoryId.HasValue
            ? await _productService.GetByCategoryIdAsync(categoryId.Value, cancellationToken)
            : await _productService.GetAllAsync(cancellationToken);

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
            return NotFound(new { message = $"Producto con Id '{id}' no encontrado." });

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, dto, cancellationToken);
            if (product is null)
                return NotFound(new { message = $"Producto con Id '{id}' no encontrado." });

            return Ok(product);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"Producto con Id '{id}' no encontrado." });

        return NoContent();
    }
}
