using Newtonsoft.Json.Linq;

namespace FuturePrelude.Quartz;

/// <summary> 用于将 API 响应统一包装为 <see cref="ApiResponse" /> 格式 </summary>
public class ResponseWrapperMiddleware
{
    private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        .SetJsonSerializerSettings();

    private readonly RequestDelegate _next;

    public ResponseWrapperMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #region private methods

    /// <summary> 检查端点是否标记了 <see cref="SkipResponseWrapAttribute" /> </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static bool HasSkipResponseWrapMetadata(HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<SkipResponseWrapAttribute>() != null;

    /// <summary> 根据请求路径判断是否应跳过包装（如 Swagger 和 favicon） </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static bool ShouldSkipByPath(HttpContext context)
    {
        if (context.IsWebSocketRequest() || context.Request.Path.StartsWithSegments("/hubs"))
        {
            return true;
        }

        var path = context.Request.Path.Value;

        return path?.Contains("swagger", StringComparison.OrdinalIgnoreCase) == true
            || path?.Contains("favicon", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary> 判断是否应跳过响应包装 </summary>
    /// <param name="context"> </param>
    /// <param name="responseBodyStream"> </param>
    /// <returns> </returns>
    private static bool ShouldSkipWrapping(HttpContext context, Stream responseBodyStream)
    {
        if (ShouldSkipByPath(context) || HasSkipResponseWrapMetadata(context))
        {
            return true;
        }

        // 204/304 或空响应体无需包装
        if (context.Response.StatusCode == StatusCodes.Status204NoContent
            || context.Response.StatusCode == StatusCodes.Status304NotModified
            || responseBodyStream.Length == 0)
        {
            return true;
        }

        return !IsJsonContentType(context.Response.ContentType);
    }

    /// <summary> 判断 Content-Type 是否为 JSON 类型 </summary>
    /// <param name="contentType"> </param>
    /// <returns> </returns>
    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("text/json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("+json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary> 判断响应体是否已是 <see cref="ApiResponse" /> 格式（避免双重包装） </summary>
    /// <param name="body"> </param>
    /// <returns> </returns>
    private static bool IsApiResponse(string body)
    {
        return TryParseJsonObject(body, out var jsonObject)
            && HasProperty(jsonObject, nameof(ApiResponse.Code))
            && HasProperty(jsonObject, nameof(ApiResponse.Message))
            && HasProperty(jsonObject, nameof(ApiResponse.Data))
            && HasProperty(jsonObject, nameof(ApiResponse.Timestamp));
    }

    /// <summary> 检查 JObject 是否包含指定属性名（不区分大小写） </summary>
    /// <param name="jsonObject"> </param>
    /// <param name="propertyName"> </param>
    /// <returns> </returns>
    private static bool HasProperty(JObject jsonObject, string propertyName)
    {
        return jsonObject.Properties()
            .Any(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary> 从响应体中读取数据，支持 JSON 和纯文本 </summary>
    /// <param name="body"> </param>
    /// <returns> </returns>
    private static object? ReadResponseData(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        return TryParseJson(body)
            ? new JRaw(body)
            : body;
    }

    /// <summary> 尝试将字符串解析为有效的 JSON </summary>
    /// <param name="body"> </param>
    /// <returns> </returns>
    private static bool TryParseJson(string body)
    {
        try
        {
            _ = JToken.Parse(body);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    /// <summary> 尝试将字符串解析为 JObject </summary>
    /// <param name="body"> </param>
    /// <param name="jsonObject"> </param>
    /// <returns> </returns>
    private static bool TryParseJsonObject(string body, out JObject? jsonObject)
    {
        jsonObject = null;

        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            jsonObject = JToken.Parse(body) as JObject;
            return jsonObject != null;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }

    /// <summary> 将响应体从内存流复制到原始响应流 </summary>
    /// <param name="source"> </param>
    /// <param name="destination"> </param>
    /// <returns> </returns>
    private static async Task CopyResponseBodyAsync(Stream source, Stream destination)
    {
        source.Seek(0, SeekOrigin.Begin);
        await source.CopyToAsync(destination);
    }

    #endregion private methods

    public async Task Invoke(HttpContext context)
    {
        var originalBody = context.Response.Body;

        // 将响应体重定向到内存流以拦截内容
        await using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        // 恢复原始响应流
        context.Response.Body = originalBody;

        if (ShouldSkipWrapping(context, memoryStream))
        {
            await CopyResponseBodyAsync(memoryStream, originalBody);
            return;
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(memoryStream).ReadToEndAsync();

        // 已包装的响应直接透传
        if (IsApiResponse(body))
        {
            await CopyResponseBodyAsync(memoryStream, originalBody);
            return;
        }

        // 包装为标准 ApiResponse 格式
        var result = new ApiResponse<object>();
        if (context.Response.StatusCode == 200)
        {
            result = ApiResponse<object>.Ok(ReadResponseData(body));
        }
        else
        {
            result = ApiResponse<object>.Fail(context.Response.StatusCode, body);
        }

        result.TraceId = context.TraceIdentifier;
        var wrappedBody = JsonConvert.SerializeObject(result, SerializerSettings);
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength = Encoding.UTF8.GetByteCount(wrappedBody);

        await context.Response.WriteAsync(wrappedBody);
    }
}
