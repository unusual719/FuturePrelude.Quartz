namespace FuturePrelude.Quartz.Core;

/// <summary> 统一 API 响应格式 </summary>
public class ApiResponse<T>
{
    /// <summary> 业务状态码，200 表示成功 </summary>
    public int Code { get; set; }

    /// <summary> 响应消息 </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary> 响应数据 </summary>
    public T? Data { get; set; }

    /// <summary> 时间戳（毫秒） </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary> 请求追踪 ID </summary>
    public string? TraceId { get; set; }

    /// <summary> 创建成功响应 </summary>
    /// <param name="data"> </param>
    /// <param name="message"> </param>
    /// <returns> </returns>
    public static ApiResponse<T> Ok(T? data = default, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary> 创建失败响应 </summary>
    /// <param name="code"> </param>
    /// <param name="message"> </param>
    /// <returns> </returns>
    public static ApiResponse<T> Fail(int code, string message)
    {
        return new ApiResponse<T>
        {
            Code = code,
            Message = message,
            Data = default,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}

/// <summary> 非泛型版本的 API 响应 </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary> 创建成功响应 </summary>
    /// <param name="message"> </param>
    /// <returns> </returns>
    public static ApiResponse Ok(string message = "操作成功")
    {
        return new ApiResponse
        {
            Code = 200,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary> 创建失败响应 </summary>
    /// <param name="code"> </param>
    /// <param name="message"> </param>
    /// <returns> </returns>
    public static ApiResponse Fail(int code, string message)
    {
        return new ApiResponse
        {
            Code = code,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
