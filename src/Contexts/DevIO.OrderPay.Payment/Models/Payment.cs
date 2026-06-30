using System.Diagnostics.CodeAnalysis;
using DevIO.OrderPay.Payment.Exceptions;
using DevIO.OrderPay.SharedKernel;
using DevIO.OrderPay.SharedKernel.Contracts;

namespace DevIO.OrderPay.Payment.Models;

public class Payment : AggregateRoot
{
    // The aggregate is the ONLY place Status changes — every move is checked
    // against this map so illegal transitions can't reach the database.
    private static readonly Dictionary<PaymentStatus, PaymentStatus[]> _allowedTransitions = new()
    {
        [PaymentStatus.Pending]    = [PaymentStatus.Processing, PaymentStatus.Failed],
        [PaymentStatus.Processing] = [PaymentStatus.Authorized, PaymentStatus.Pending, PaymentStatus.Failed],
        [PaymentStatus.Authorized] = [PaymentStatus.Captured, PaymentStatus.Failed],
        [PaymentStatus.Captured]   = [PaymentStatus.Refunded],
        [PaymentStatus.Refunded]   = [],
        [PaymentStatus.Failed]     = [],
    };

    public Payment() { }

    [SetsRequiredMembers]
    public Payment(Guid orderId, Amount amount, PaymentMethod method)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public required Guid Id { get; set; }
    public required Guid OrderId { get; set; }
    public required Amount Amount { get; set; }
    public required PaymentMethod Method { get; set; }
    public required PaymentStatus Status { get; set; }
    public string? GatewayReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public void BeginProcessing() => TransitionTo(PaymentStatus.Processing);

    public void Authorize(string gatewayReference)
    {
        TransitionTo(PaymentStatus.Authorized);
        GatewayReference = gatewayReference;
    }

    public void Capture()
    {
        TransitionTo(PaymentStatus.Captured);
        // Published via the Outbox; the Order context advances the order on this event.
        RaiseEvent(new PaymentCapturedEvent(Id, OrderId, Amount.Value, Amount.Currency, GatewayReference ?? string.Empty));
    }

    // Soft fail — a declined attempt returns the payment to Pending so it can be retried.
    public void Decline() => TransitionTo(PaymentStatus.Pending);

    // Hard fail — deliberate give-up (max attempts, cancellation); terminal.
    public void Abandon() => TransitionTo(PaymentStatus.Failed);

    public void Refund() => TransitionTo(PaymentStatus.Refunded);

    private void TransitionTo(PaymentStatus next)
    {
        if (!_allowedTransitions[Status].Contains(next))
            throw new InvalidPaymentTransitionException(Status, next);

        Status = next;
        UpdatedAt = DateTime.UtcNow;
    }
}
