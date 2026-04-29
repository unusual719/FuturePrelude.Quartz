using Polly;
using Polly.Contrib.WaitAndRetry;

namespace FuturePrelude.Quartz;

/// <summary> Policy 策略工厂 </summary>
public class PolicyFactory
{
    private readonly ILogger<PolicyFactory> _logger;

    public PolicyFactory(ILogger<PolicyFactory> logger) => _logger = logger;

    private static bool IsTransientException(Exception ex)
    {
        if (ex is HttpRequestException httpEx && httpEx.StatusCode.HasValue)
        {
            var code = (int)httpEx.StatusCode.Value;
            return code is 429 or (>= 500 and <= 599);
        }

        // IOException / SocketException are often transient network issues
        return ex is System.IO.IOException or System.Net.Sockets.SocketException;
    }

    /// <summary> 创建重试策略 </summary>
    /// <typeparam name="T"> </typeparam>
    /// <param name="retryCount"> 最大重试次数（不包含首次执行） </param>
    /// <param name="baseDelaySeconds"> 基础延迟时间（单位：秒） </param>
    /// <param name="maxDelaySeconds"> 最大延迟时间（单位：秒） </param>
    /// <param name="timeoutSeconds"> </param>
    /// <param name="shouldRetryResult"> 命中该结果条件时触发重试；为空则仅对异常重试 </param>
    /// <returns> </returns>
    public IAsyncPolicy<T> CreateRetryPolicy<T>(int retryCount = 3,
        int baseDelaySeconds = 2,
        int maxDelaySeconds = 30,
        int timeoutSeconds = 15,
        Func<T, bool>? shouldRetryResult = null)
    {
        string cacheKey = $"Retry_{typeof(T).Name}_{retryCount}_{baseDelaySeconds}";
        var finalRetryCount = Math.Clamp(retryCount, 0, 10);
        var baseDelay = TimeSpan.FromSeconds(Math.Max(baseDelaySeconds, 1));
        var maxDelay = TimeSpan.FromSeconds(Math.Max(maxDelaySeconds, baseDelaySeconds));

        // 生成带抖动(Jitter)的退避序列，并限制最大延迟
        var delaySequence = Backoff
            .DecorrelatedJitterBackoffV2(baseDelay, finalRetryCount)
            .Select(d => d > maxDelay ? maxDelay : d);

        PolicyBuilder<T> retryBuilder = Policy<T>
            .Handle<TimeoutException>()
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested)
            .Or<Exception>(ex => IsTransientException(ex));

        if (shouldRetryResult != null)
        {
            retryBuilder = retryBuilder.OrResult(shouldRetryResult);
        }

        var retryPolicy = retryBuilder.WaitAndRetryAsync(
                sleepDurations: delaySequence,
                onRetry: (outcome, timespan, attempt, _) =>
                {
                    if (outcome.Result is IDisposable disposableResult)
                    {
                        disposableResult.Dispose();
                    }

                    var exception = outcome.Exception;
                    var message = exception?.Message
                        ?? (outcome.Result is HttpResponseMessage response
                            ? $"HTTP {(int)response.StatusCode}"
                            : "结果命中重试条件");
                    _logger.LogWarning(
                        outcome.Exception,
                        "操作执行失败，准备进行第 {Attempt} 次重试。等待延迟: {Delay}ms，Error：{Message}",
                        attempt,
                        timespan.TotalMilliseconds,
                        message);
                });

        if (timeoutSeconds <= 0)
        {
            return retryPolicy;
        }

        var timeoutPolicy = Policy.TimeoutAsync<T>(timeoutSeconds);
        return Policy.WrapAsync(retryPolicy, timeoutPolicy);
    }
}