namespace DevIO.OrderPay.Core.Gateway;

// Port over the logistics provider (real carrier / in-app mock). Lives in Core —
// like IPaymentGateway and the repository interfaces — so the Infrastructure adapter
// (HttpLogisticsClient) implements it without referencing the Application layer, and
// the Application handler can depend on it without touching Infrastructure.
//
// NotifyOrderAsync tells the carrier an order is ready to ship. It returns no result:
// the adapter throws on a non-2xx response so the Outbox worker (at-least-once) retries
// the whole event, and the dispatch's IdempotencyKey lets the carrier dedupe a replay.
public interface ILogisticsClient
{
    Task NotifyOrderAsync(LogisticsDispatch dispatch, CancellationToken cancellationToken = default);
}
