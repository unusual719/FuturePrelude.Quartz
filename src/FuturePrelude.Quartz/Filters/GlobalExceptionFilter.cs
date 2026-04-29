namespace FuturePrelude.Quartz;

/// <summary> 全局统一异常处理筛选器，统一返回格式化的错误响应 </summary>
public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    /// <summary> 初始化全局异常筛选器 </summary>
    /// <param name="logger"> 日志记录器 </param>
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    /// <summary> 从 HttpContext.Items 中获取 Action 执行时的参数 </summary>
    /// <param name="httpContext"> </param>
    /// <returns> </returns>
    private static IReadOnlyDictionary<string, object?> GetActionArguments(HttpContext httpContext)
    {
        // 从 HttpContext.Items 中尝试获取存储的 Action 参数
        if (!httpContext.Items.TryGetValue(RequestBodyCaptureMiddleware.ItemKey, out var rawValue) || rawValue == null)
        {
            return new Dictionary<string, object?>();
        }

        // 根据不同类型进行转换
        return rawValue switch
        {
            IReadOnlyDictionary<string, object?> readOnlyArguments => readOnlyArguments,
            IDictionary<string, object?> nullableArguments => new Dictionary<string, object?>(nullableArguments, StringComparer.OrdinalIgnoreCase),
            _ => new Dictionary<string, object?>()
        };
    }

    /// <summary> 构建异常发生时的参数快照，用于日志记录 包含路由参数、查询参数和 Action 参数 </summary>
    /// <param name="context"> </param>
    /// <param name="actionDescriptor"> </param>
    /// <returns> </returns>
    private static IReadOnlyDictionary<string, object?> BuildParameterSnapshot(ExceptionContext context, ControllerActionDescriptor actionDescriptor)
    {
        var snapshot = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // 收集路由参数（如 {id}）
        foreach (var routeValue in context.RouteData.Values)
        {
            if (routeValue.Key is string key && !snapshot.ContainsKey(key))
            {
                snapshot[key] = routeValue.Value;
            }
        }

        // 收集查询字符串参数（如 ?name=value）
        foreach (var query in context.HttpContext.Request.Query)
        {
            if (!snapshot.ContainsKey(query.Key))
            {
                snapshot[query.Key] = query.Value.Count switch
                {
                    0 => null,
                    1 => query.Value[0],
                    _ => query.Value.ToArray()
                };
            }
        }

        // 收集 Action 方法参数（Body、Query 等）
        var actionArguments = GetActionArguments(context.HttpContext);
        if (actionArguments.Count == 0) return snapshot;

        foreach (var parameter in actionDescriptor.MethodInfo.GetParameters())
        {
            if (string.IsNullOrWhiteSpace(parameter.Name)) continue;
            if (!actionArguments.TryGetValue(parameter.Name, out var value)) continue;
            // 跳过文件上传等不适合日志记录的类型
            if (ShouldSkipLogging(value)) continue;

            snapshot[parameter.Name] = value;
        }

        return snapshot;
    }

    /// <summary> 判断值是否应该跳过日志记录（如文件上传） </summary>
    /// <param name="value"> </param>
    /// <returns> </returns>
    private static bool ShouldSkipLogging(object? value)
    {
        return value switch
        {
            IFormFile => true, // 单个文件上传
            IEnumerable<IFormFile> => true, // 多个文件上传
            _ => false
        };
    }

    /// <summary> 拼接多个描述字符串，去除空值 </summary>
    /// <param name="values"> </param>
    /// <returns> </returns>
    private static string JoinDescription(params string?[] values) => string.Join(" - ", values.Where(static value => !string.IsNullOrWhiteSpace(value)));

    /// <summary> 从 Controller 类型解析描述信息 </summary>
    /// <param name="controllerTypeInfo"> Controller 类型信息 </param>
    /// <returns> 描述字符串或 null </returns>
    private static string? ResolveControllerDescription(MemberInfo controllerTypeInfo)
    {
        // 优先获取 ApiExplorerSettings 中的 GroupName
        var apiExplorerSettings = controllerTypeInfo.GetCustomAttribute<ApiExplorerSettingsAttribute>(inherit: true);
        if (!string.IsNullOrWhiteSpace(apiExplorerSettings?.GroupName))
        {
            return apiExplorerSettings.GroupName;
        }

        // 其次获取 DescriptionAttribute
        return controllerTypeInfo.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;
    }

    /// <summary> 安全序列化对象 </summary>
    /// <param name="value"> </param>
    /// <returns> </returns>
    private static string SerializeSafely(object value)
    {
        try
        {
            return JsonConvert.SerializeObject(value);
        }
        catch
        {
            return "\"<参数序列化失败>\"";
        }
    }

    /// <summary> 根据异常类型解析对应的 HTTP 状态码、业务错误码和消息 </summary>
    /// <param name="exception"> </param>
    /// <returns> </returns>
    private static (int StatusCode, int ErrorCode, string Message) ResolveExceptionInfo(Exception exception)
    {
        if (IsBusinessException(exception))
        {
            return (StatusCodes.Status200OK, -1, exception.Message);
        }

        return exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, 401, "未授权访问"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, 404, "请求的资源不存在"),
            InvalidOperationException => (StatusCodes.Status400BadRequest, 400, "操作无效"),
            TimeoutException => (StatusCodes.Status500InternalServerError, 500, "操作超时"),
            _ => (StatusCodes.Status500InternalServerError, 500, exception.Message)
        };
    }

    /// <summary> 判断异常是否属于业务异常类型 </summary>
    /// <param name="exception"> </param>
    /// <returns> </returns>
    private static bool IsBusinessException(Exception exception)
    {
        for (var current = exception.GetType(); current != null; current = current.BaseType)
        {
            if (string.Equals(current.Name, "BusinessException", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary> 记录异常日志 </summary>
    /// <param name="context"> </param>
    /// <param name="exception"> </param>
    private void LogException(ExceptionContext context, Exception exception)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            return;

        // 构建参数快照
        var parameterValues = BuildParameterSnapshot(context, actionDescriptor);
        // 获取 Controller 和 Action 的描述信息
        var controllerDescription = ResolveControllerDescription(actionDescriptor.ControllerTypeInfo);
        var actionDescription = actionDescriptor.MethodInfo.GetCustomAttribute<DisplayNameAttribute>(inherit: true)?.DisplayName;

        // 记录错误日志
        _logger.LogError(
            exception,
            "[Api.Controller.Exception] Controller={Controller} Action={Action} Description={Description} Params={Params}",
            actionDescriptor.ControllerName,
            actionDescriptor.ActionName,
            JoinDescription(controllerDescription, actionDescription),
            SerializeSafely(parameterValues));
    }

    /// <summary> 异步异常处理入口 </summary>
    /// <param name="context"> </param>
    public Task OnExceptionAsync(ExceptionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // WebSocket 请求不处理
        if (context.HttpContext.IsWebSocketRequest()) return Task.CompletedTask;
        // Razor Page 不处理
        if (context.ActionDescriptor is CompiledPageActionDescriptor) return Task.CompletedTask;

        var exception = context.Exception;
        // 解析异常信息
        var (statusCode, errorCode, message) = ResolveExceptionInfo(exception);

        // 构建统一响应格式
        var response = new ApiResponse<object>
        {
            Code = errorCode,
            Message = message,
            TraceId = context.HttpContext.TraceIdentifier,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Data = null
        };

        // 记录异常日志
        LogException(context, exception);

        // 标记异常已处理
        context.ExceptionHandled = true;
        context.HttpContext.Response.StatusCode = statusCode;
        context.Result = new JsonResult(response);

        return Task.CompletedTask;
    }
}
