using System.Net.Http.Json;
using DevIO.OrderPay.Core.Gateway;

namespace DevIO.OrderPay.Infra;

// HTTP adapter for ILogisticsClient. Registered as a typed HttpClient (BaseAddress +
// X-Api-Key set at registration in Program.cs). Throws on a non-2xx response so the
// Outbox worker retries the whole event; the dispatch IdempotencyKey lets the carrier
// dedupe the replay so a retry never creates a duplicate shipment.
public class HttpLogisticsClient(HttpClient http) : ILogisticsClient
{
    public async Task NotifyOrderAsync(LogisticsDispatch dispatch, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await http.PostAsJsonAsync("orders", dispatch, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
