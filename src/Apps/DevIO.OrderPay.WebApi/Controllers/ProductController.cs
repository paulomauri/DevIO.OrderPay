using Asp.Versioning;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevIO.OrderPay.WebApi.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.GeneralPolicy)]
public class ProductController(IProductService service) : ControllerBase
{
    private readonly IProductService _service = service;

    [HttpGet]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(IEnumerable<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _service.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitingExtensions.WritesPolicy)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post([FromBody] ProductRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var product = await _service.AddAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitingExtensions.WritesPolicy)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Put(Guid id, [FromBody] ProductRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var product = await _service.UpdateAsync(id, request);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitingExtensions.WritesPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool result = await _service.DeleteAsync(id);
        return result ? Ok() : NotFound();
    }

    [HttpPatch("{id:guid}/sku")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting(RateLimitingExtensions.WritesPolicy)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSku(Guid id, [FromBody] string sku)
    {
        var product = await _service.UpdateSkuAsync(id, sku);
        return product is null ? NotFound() : Ok(product);
    }
}
