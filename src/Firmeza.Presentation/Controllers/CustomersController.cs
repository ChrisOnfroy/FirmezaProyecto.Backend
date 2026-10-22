using Firmeza.Application.DTOs.Customers;
using Firmeza.Application.Interfaces.Services;
using Firmeza.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Firmeza.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var customers = await _customerService.GetAllAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            return NotFound(new { message = $"Cliente con Id '{id}' no encontrado." });

        return Ok(customer);
    }

    [HttpGet("by-identification/{identificationNumber}")]
    public async Task<IActionResult> GetByIdentification(string identificationNumber, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdentificationAsync(identificationNumber, cancellationToken);
        if (customer is null)
            return NotFound(new { message = $"Cliente con documento '{identificationNumber}' no encontrado." });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var deactivated = await _customerService.DeactivateAsync(id, cancellationToken);
        if (!deactivated)
            return NotFound(new { message = $"Cliente con Id '{id}' no encontrado." });

        return NoContent();
    }
}
