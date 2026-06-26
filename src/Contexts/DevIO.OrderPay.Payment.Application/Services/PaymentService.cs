using DevIO.OrderPay.Core.Gateway;
using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Payment.Application.DTOs;
using DevIO.OrderPay.Payment.Application.Integration;
using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Payment.Application.Services;

public class PaymentService(
    IPaymentRepository repository,
    IPaymentGateway gateway,
    IPaymentCapturedHandler capturedHandler) : IPaymentService
{
    private readonly IPaymentRepository _repository = repository;
    private readonly IPaymentGateway _gateway = gateway;
    private readonly IPaymentCapturedHandler _capturedHandler = capturedHandler;

    public async Task<PaymentResponse> PayAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PaymentAttempt> attempts =
            [.. await _repository.GetAttemptsByOrderIdAsync(request.OrderId)];

        // A caller that reuses the same attempt number reuses the same key, which
        // is what makes a retry idempotent. Omitting it starts a fresh attempt.
        int attemptNumber = request.AttemptNumber ?? PaymentAttempt.NextNumber(attempts);
        string idempotencyKey = $"{request.OrderId}:{attemptNumber}";

        Payment.Models.Payment? payment = await _repository.GetByOrderIdAsync(request.OrderId);
        PaymentAttempt? existing = await _repository.GetAttemptByKeyAsync(idempotencyKey);

        // Idempotent replay: this attempt already resolved — return the stored
        // result without charging again.
        if (existing is not null && existing.Outcome != PaymentAttemptOutcome.Pending)
            return MapToResponse(payment!, existing);

        Amount amount = new(request.Amount, request.Currency);
        PaymentMethod method = new PaymentMethodCard(request.CardBrand, request.Last4, request.Expiry, request.Type);

        bool isNewPayment = payment is null;
        payment ??= new Payment.Models.Payment(request.OrderId, amount, method);

        PaymentAttempt attempt = existing ?? new PaymentAttempt(request.OrderId, attemptNumber);

        // Persist the attempt (and a brand-new Payment) BEFORE calling the gateway.
        // The unique index on IdempotencyKey is what guarantees at-most-once: a
        // concurrent or replayed call can't create a second attempt for this key.
        if (existing is null)
        {
            await _repository.AddAttemptAsync(attempt);
            if (isNewPayment)
                await _repository.AddAsync(payment);
            await _repository.SaveChangesAsync();
        }

        if (payment.Status == PaymentStatus.Pending)
            payment.BeginProcessing();

        PaymentGatewayResult result = await _gateway.ChargeAsync(idempotencyKey, amount, method, cancellationToken);

        if (result.Approved)
        {
            attempt.RecordSuccess(result.GatewayReference!);
            payment.Authorize(result.GatewayReference!);
            payment.Capture();
        }
        else
        {
            attempt.RecordFailure();
            payment.Decline();
        }

        await _repository.UpdateAsync(payment);
        await _repository.SaveChangesAsync();

        if (result.Approved)
        {
            await _capturedHandler.HandleAsync(
                new PaymentCapturedEvent(payment.Id, payment.OrderId, amount.Value, amount.Currency, result.GatewayReference!),
                cancellationToken);
        }

        return MapToResponse(payment, attempt);
    }

    public async Task<PaymentResponse?> GetByOrderIdAsync(Guid orderId)
    {
        Payment.Models.Payment? payment = await _repository.GetByOrderIdAsync(orderId);
        if (payment is null) return null;

        IEnumerable<PaymentAttempt> attempts = await _repository.GetAttemptsByOrderIdAsync(orderId);
        PaymentAttempt? latest = attempts.OrderByDescending(a => a.AttemptNumber).FirstOrDefault();

        return MapToResponse(payment, latest);
    }

    private static PaymentResponse MapToResponse(Payment.Models.Payment payment, PaymentAttempt? attempt) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        Amount = payment.Amount.Value,
        Currency = payment.Amount.Currency,
        Status = payment.Status.ToString(),
        GatewayReference = payment.GatewayReference,
        AttemptNumber = attempt?.AttemptNumber ?? 0,
        AttemptOutcome = attempt?.Outcome.ToString() ?? string.Empty,
    };
}
