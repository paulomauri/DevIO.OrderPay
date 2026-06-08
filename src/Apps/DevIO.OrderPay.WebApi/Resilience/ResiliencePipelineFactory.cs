using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace DevIO.OrderPay.WebApi.Resilience;

internal static class ResiliencePipelineFactory
{
    // Production defaults. Tests override delays/timeouts to keep runs fast.
    internal static ResiliencePipeline<HttpResponseMessage> CreateKeycloakPipeline(
        int        maxRetryAttempts   = 3,
        TimeSpan?  retryDelay         = null,
        TimeSpan?  timeout            = null,
        double     failureRatio       = 0.5,
        int        minimumThroughput  = 3,
        TimeSpan?  breakDuration      = null,
        TimeSpan?  samplingDuration   = null)
    {
        var shouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(r => (int)r.StatusCode >= 500);

        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddTimeout(timeout ?? TimeSpan.FromSeconds(15));

        if (maxRetryAttempts > 0)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = maxRetryAttempts,
                Delay            = retryDelay ?? TimeSpan.FromSeconds(1),
                BackoffType      = DelayBackoffType.Exponential,
                ShouldHandle     = shouldHandle
            });
        }

        return builder
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio      = failureRatio,
                SamplingDuration  = samplingDuration ?? TimeSpan.FromSeconds(30),
                MinimumThroughput = minimumThroughput,
                BreakDuration     = breakDuration ?? TimeSpan.FromSeconds(30),
                ShouldHandle      = shouldHandle
            })
            .Build();
    }
}
