using DevIO.OrderPay.WebApi.Resilience;
using FluentAssertions;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net;

namespace DevIO.OrderPay.Tests.Resilience;

public class PollyResilienceTests
{
    // Each test builds its own pipeline instance — circuit breaker state never leaks.

    // ── Retry ────────────────────────────────────────────────

    [Fact]
    public async Task Retry_OnHttpRequestException_CallsHandlerOncePerAttempt()
    {
        int callCount = 0;
        var pipeline  = BuildRetryOnly(maxRetryAttempts: 2);

        var act = async () => await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            throw new HttpRequestException("simulated failure");
#pragma warning disable CS0162
            return new ValueTask<HttpResponseMessage>(new HttpResponseMessage());
#pragma warning restore CS0162
        });

        await act.Should().ThrowAsync<HttpRequestException>();
        callCount.Should().Be(3); // 1 initial + 2 retries
    }

    [Fact]
    public async Task Retry_On5xxResponse_CallsHandlerOncePerAttempt()
    {
        int callCount = 0;
        var pipeline  = BuildRetryOnly(maxRetryAttempts: 2);

        await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            return new ValueTask<HttpResponseMessage>(
                new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        });

        callCount.Should().Be(3); // 1 initial + 2 retries
    }

    [Fact]
    public async Task Retry_OnSuccess_ExecutesOnce()
    {
        int callCount = 0;
        var pipeline  = BuildRetryOnly(maxRetryAttempts: 3);

        var result = await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            return new ValueTask<HttpResponseMessage>(
                new HttpResponseMessage(HttpStatusCode.OK));
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task Retry_SucceedsOnSecondAttempt_StopsRetrying()
    {
        int callCount = 0;
        var pipeline  = BuildRetryOnly(maxRetryAttempts: 3);

        var result = await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            var status = callCount == 1
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;
            return new ValueTask<HttpResponseMessage>(new HttpResponseMessage(status));
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().Be(2); // failed once, succeeded on retry
    }

    [Fact]
    public async Task Retry_On4xxResponse_DoesNotRetry()
    {
        int callCount = 0;
        var pipeline  = BuildRetryOnly(maxRetryAttempts: 3);

        var result = await pipeline.ExecuteAsync(ct =>
        {
            callCount++;
            return new ValueTask<HttpResponseMessage>(
                new HttpResponseMessage(HttpStatusCode.BadRequest));
        });

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        callCount.Should().Be(1); // 4xx is not retriable
    }

    // ── Circuit Breaker ───────────────────────────────────────

    [Fact]
    public async Task CircuitBreaker_AfterEnoughFailures_ThrowsBrokenCircuitException()
    {
        var pipeline = BuildCircuitBreakerOnly(minimumThroughput: 3, failureRatio: 0.5);

        // Fire 4 failures — 100% failure rate, exceeds both threshold and ratio
        for (int i = 0; i < 4; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(ct =>
                    new ValueTask<HttpResponseMessage>(
                        new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
            }
            catch { }
        }

        // Circuit is now open — next call should be rejected immediately
        var act = async () => await pipeline.ExecuteAsync(ct =>
            new ValueTask<HttpResponseMessage>(
                new HttpResponseMessage(HttpStatusCode.OK)));

        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task CircuitBreaker_WhenOpen_RejectsWithoutCallingInnerHandler()
    {
        int callCount = 0;
        var pipeline  = BuildCircuitBreakerOnly(minimumThroughput: 3, failureRatio: 0.5);

        // Trip the circuit
        for (int i = 0; i < 4; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(ct =>
                    new ValueTask<HttpResponseMessage>(
                        new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
            }
            catch { }
        }

        // Reset counter — open circuit should NOT call the delegate
        callCount = 0;

        try
        {
            await pipeline.ExecuteAsync(ct =>
            {
                callCount++;
                return new ValueTask<HttpResponseMessage>(
                    new HttpResponseMessage(HttpStatusCode.OK));
            });
        }
        catch (BrokenCircuitException) { }

        callCount.Should().Be(0);
    }

    // ── Timeout ───────────────────────────────────────────────

    [Fact]
    public async Task Timeout_WhenExceeded_ThrowsTimeoutRejectedException()
    {
        var pipeline = BuildTimeoutOnly(timeout: TimeSpan.FromMilliseconds(50));

        var act = async () => await pipeline.ExecuteAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task Timeout_WhenWithinLimit_Succeeds()
    {
        var pipeline = BuildTimeoutOnly(timeout: TimeSpan.FromSeconds(5));

        var result = await pipeline.ExecuteAsync(ct =>
            new ValueTask<HttpResponseMessage>(
                new HttpResponseMessage(HttpStatusCode.OK)));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Helpers ───────────────────────────────────────────────

    // Isolated retry pipeline — minimumThroughput high so circuit never opens.
    private static Polly.ResiliencePipeline<HttpResponseMessage> BuildRetryOnly(int maxRetryAttempts) =>
        ResiliencePipelineFactory.CreateKeycloakPipeline(
            maxRetryAttempts:  maxRetryAttempts,
            retryDelay:        TimeSpan.Zero,
            timeout:           TimeSpan.FromSeconds(30),
            minimumThroughput: 1000,             // circuit never trips during retry tests
            samplingDuration:  TimeSpan.FromSeconds(30),
            breakDuration:     TimeSpan.FromSeconds(30));

    // Isolated circuit breaker pipeline — zero retries so each failure counts as one call.
    private static Polly.ResiliencePipeline<HttpResponseMessage> BuildCircuitBreakerOnly(
        int minimumThroughput, double failureRatio) =>
        ResiliencePipelineFactory.CreateKeycloakPipeline(
            maxRetryAttempts:  0,
            retryDelay:        TimeSpan.Zero,
            timeout:           TimeSpan.FromSeconds(30),
            failureRatio:      failureRatio,
            minimumThroughput: minimumThroughput,
            samplingDuration:  TimeSpan.FromSeconds(30),
            breakDuration:     TimeSpan.FromSeconds(30));

    // Isolated timeout pipeline — zero retries, circuit never trips.
    private static Polly.ResiliencePipeline<HttpResponseMessage> BuildTimeoutOnly(TimeSpan timeout) =>
        ResiliencePipelineFactory.CreateKeycloakPipeline(
            maxRetryAttempts:  0,
            retryDelay:        TimeSpan.Zero,
            timeout:           timeout,
            minimumThroughput: 1000,
            samplingDuration:  TimeSpan.FromSeconds(30),
            breakDuration:     TimeSpan.FromSeconds(30));
}
