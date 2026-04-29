namespace FuturePrelude.Quartz;

/// <summary> 为每个请求注入统一的 RequestId，并写入 NLog 上下文 </summary>
public class NLogRequestIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary> 请求头中用于透传请求标识的标准键名 </summary>
    public const string RequestIdHeaderName = "X-Request-Id";

    public NLogRequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary> 获取客户端 IP </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static string ResolveClientIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary> 注入 RequestId 到 HttpContext、响应头与日志上下文，确保整条请求链路可追踪 同时写入 Path / Method / ClientIp，便于按请求维度检索日志 </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = ResolveRequestId(context);
        var requestPath = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        var requestMethod = context.Request.Method;
        var clientIp = ResolveClientIp(context);

        // 同步到 HttpContext，便于后续中间件、异常处理和框架日志统一使用
        context.TraceIdentifier = requestId;
        context.Items["RequestId"] = requestId;

        // 回写响应头，便于客户端和日志平台进行链路排查
        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (ScopeContext.PushProperty("RequestId", requestId))
        using (ScopeContext.PushProperty("RequestPath", requestPath))
        using (ScopeContext.PushProperty("RequestMethod", requestMethod))
        using (ScopeContext.PushProperty("ClientIp", clientIp))
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var elapsed = sw.ElapsedMilliseconds;

                using (ScopeContext.PushProperty("Elapsed", $"{elapsed}ms"))
                {
                }
            }
        }

        static string ResolveRequestId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var headerValue))
            {
                var requestIdFromHeader = headerValue.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(requestIdFromHeader))
                {
                    return requestIdFromHeader;
                }
            }

            if (!string.IsNullOrWhiteSpace(context.TraceIdentifier))
            {
                return context.TraceIdentifier;
            }

            return Guid.NewGuid().ToString("N").ToUpperInvariant();
        }
    }
}