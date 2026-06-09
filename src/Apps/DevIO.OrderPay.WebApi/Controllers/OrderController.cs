using Asp.Versioning;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Exceptions;
using DevIO.OrderPay.Order.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevIO.OrderPay.WebApi.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class OrderController(IOrderService service) : ControllerBase
{
    private readonly IOrderService _service = service;

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _service.GetAllAsync();
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _service.GetByIdAsync(id);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("customer/{customerId:guid}")]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        var orders = await _service.GetByCustomerIdAsync(customerId);
        return Ok(orders);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Post([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var order = await _service.AddAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (DuplicateOrderItemException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ValueLowerThanZeroException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        bool result = await _service.DeleteAsync(id);
        return result ? Ok() : NotFound();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        var parsed = OrderStatusConverter.From(status);
        if (parsed is null)
            return BadRequest(new { error = $"Invalid status '{status}'." });

        var order = await _service.UpdateStatusAsync(id, parsed.Value);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPatch("{id:guid}/delivery-date")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDeliveryDate(Guid id, [FromBody] DateTime deliveryDate)
    {
        var order = await _service.UpdateDeliveryDateAsync(id, deliveryDate);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] OrderItemRequest item)
    {
        try
        {
            var order = await _service.AddItemAsync(id, item);
            return order is null ? NotFound() : Ok(order);
        }
        catch (DuplicateOrderItemException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ValueLowerThanZeroException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
    {
        var order = await _service.RemoveItemAsync(id, itemId);
        return order is null ? NotFound() : Ok(order);
    }
}
