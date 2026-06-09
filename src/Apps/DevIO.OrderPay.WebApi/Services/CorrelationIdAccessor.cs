namespace DevIO.OrderPay.WebApi.Services;

public class CorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    private const string Header = "X-Correlation-Id";
    private readonly IHttpContextAccessor _accessor = accessor;

    public string CorrelationId =>
        _accessor.HttpContext?.Response.Headers[Header].ToString()
        ?? Guid.NewGuid().ToString();
}
