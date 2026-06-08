namespace DevIO.OrderPay.WebApi.Services;

public class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private readonly IHttpContextAccessor _accessor;
    private const string Header = "X-Correlation-Id";

    public CorrelationIdAccessor(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string CorrelationId =>
        _accessor.HttpContext?.Response.Headers[Header].ToString()
        ?? Guid.NewGuid().ToString();
}
