namespace DevIO.OrderPay.Payment.Models;

public enum PaymentAttemptOutcome
{
    Pending,    // created; gateway call not yet resolved
    Succeeded,  // gateway authorized/captured this attempt
    Failed      // definitive decline — this attempt is done, but the Payment can retry
}
