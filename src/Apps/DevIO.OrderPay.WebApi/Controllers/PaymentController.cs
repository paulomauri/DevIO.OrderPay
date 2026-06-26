using Asp.Versioning;
using DevIO.OrderPay.Payment.Application.DTOs;
using DevIO.OrderPay.Payment.Application.Services;
using DevIO.OrderPay.Payment.Exceptions;
using DevIO.OrderPay.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DevIO.OrderPay.WebApi.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[EnableRateLimiting(RateLimitingExtensions.GeneralPolicy)]
public class PaymentController(IPaymentService service) : ControllerBase
{
    private readonly IPaymentService _service = service;

    [HttpGet("{orderId:guid}")]
    [Authorize(Policy = "AdminOrCustomer")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOrderId(Guid orderId)
    {
        PaymentResponse? payment = await _service.GetByOrderIdAsync(orderId);
        return payment is null ? NotFound() : Ok(payment);
    }

    // Pay an order. Idempotent: retrying with the same attempt number returns the
    // original result instead of charging again.
    [HttpPost]
    [Authorize(Policy = "AdminOrCustomer")]
    [EnableRateLimiting(RateLimitingExtensions.WritesPolicy)]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(PaymentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            PaymentResponse payment = await _service.PayAsync(request, cancellationToken);
            return Ok(payment);
        }
        catch (DuplicatePaymentAttemptException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}
