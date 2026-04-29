namespace FuturePrelude.Quartz;

/// <summary> 用于记录每个 HTTP 请求的执行时间 </summary>
public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    /// <summary> 初始化 <see cref="RequestTimingMiddleware" /> 实例 </summary>
    /// <param name="next"> </param>
    /// <param name="loggerFactory"> </param>
    public RequestTimingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger("RequestTimingMiddleware");
    }

    /// <summary> 执行中间件逻辑，记录请求耗时 </summary>
    /// <param name="context"> </param>
    public async Task Invoke(HttpContext context)
    {
        // 启动计时器
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 将请求传递给管道中的下一个中间件
            await _next(context);
        }
        finally
        {
            // 即使发生异常，也停止计时并记录日志
            sw.Stop();

            var elapsed = sw.ElapsedMilliseconds;
            var statusCode = context.Response?.StatusCode;

            // 记录请求耗时，格式："GET /path => 200 in 15 ms"
            _logger.LogInformation(
                "{StatusCode} in {Elapsed} ms",
                statusCode,
                elapsed
            );
        }
    }
}