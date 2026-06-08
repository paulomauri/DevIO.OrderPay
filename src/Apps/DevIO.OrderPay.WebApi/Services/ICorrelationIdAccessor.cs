namespace DevIO.OrderPay.WebApi.Services;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}
