namespace FuturePrelude.Quartz;

/// <summary> 请求体捕获中间件 读取并缓存 HTTP 请求体，以便后续 Action 可以重新读取 </summary>
/// <remarks> 主要用于日志记录和异常处理时获取原始请求数据 </remarks>
public class RequestBodyCaptureMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestBodyCaptureMiddleware> _logger;

    /// <summary> 存储请求体的 HttpContext.Items 键名 </summary>
    public const string ItemKey = "RequestBody";

    /// <summary> 初始化请求体捕获中间件 </summary>
    /// <param name="next"> </param>
    /// <param name="logger"> </param>
    public RequestBodyCaptureMiddleware(RequestDelegate next,
        ILogger<RequestBodyCaptureMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary> 执行中间件逻辑，捕获请求体 </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 只处理有请求体且可读的情况（避免 GET 等无意义的请求）
        if (context.Request.ContentLength is > 0 && context.Request.Body.CanRead)
        {
            try
            {
                // 启用缓冲，允许重新读取 Body
                context.Request.EnableBuffering();

                // 使用 StreamReader 读取请求体
                using var reader = new StreamReader(
                    context.Request.Body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: true);  // 保持 Stream 打开，以便后续继续读取

                var body = await reader.ReadToEndAsync();

                // 将请求体存储到 HttpContext.Items，供后续使用（如日志、异常记录）
                context.Items[ItemKey] = body;

                // 重置 Body 位置为起始处，让后续的 MVC ModelBinding 可以重新读取
                context.Request.Body.Position = 0;
            }
            catch (Exception ex)
            {
                // 读取失败仅记录警告，不影响主流程
                _logger.LogWarning(ex, "读取 RequestBody 失败");
            }
        }

        // 将请求传递给管道中的下一个中间件
        await _next(context);
    }
}
