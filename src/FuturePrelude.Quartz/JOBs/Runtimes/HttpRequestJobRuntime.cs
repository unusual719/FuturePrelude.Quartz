namespace FuturePrelude.Quartz;

/// <summary> HttpJob 请求构造运行时辅助方法 </summary>
internal static class HttpRequestJobRuntime
{
    /// <summary> 统一将内部请求方式枚举转换为 HttpClient 可识别的 HttpMethod </summary>
    /// <param name="method"> </param>
    /// <returns> </returns>
    /// <exception cref="NotSupportedException"> </exception>
    private static HttpMethod ToHttpMethod(HttpRequestMethod method)
    {
        return method switch
        {
            HttpRequestMethod.GET => HttpMethod.Get,
            HttpRequestMethod.POST => HttpMethod.Post,
            HttpRequestMethod.PUT => HttpMethod.Put,
            HttpRequestMethod.DELETE => HttpMethod.Delete,
            HttpRequestMethod.PATCH => HttpMethod.Patch,
            _ => throw new NotSupportedException($"不支持的请求方法: {method}")
        };
    }

    /// <summary> 解析并校验请求方式字符串，避免非法值在真正发请求时才暴露 </summary>
    /// <param name="method"> </param>
    /// <returns> </returns>
    /// <exception cref="JobExecutionException"> </exception>
    internal static HttpRequestMethod ParseMethod(string method)
    {
        if (!Enum.TryParse<HttpRequestMethod>(method, ignoreCase: true, out var action))
        {
            throw new JobExecutionException($"无效的请求方式: {method}");
        }

        return action;
    }

    /// <summary> 将 JSON 格式的 Header 配置反序列化为键值对，空白内容按未配置处理 </summary>
    /// <param name="headersJson"> </param>
    /// <returns> </returns>
    internal static Dictionary<string, string>? DeserializeHeaders(string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<Dictionary<string, string>>(headersJson.Trim());
    }

    /// <summary> 根据任务配置构造最终的 HTTP 请求对象 </summary>
    /// <param name="url"> </param>
    /// <param name="method"> </param>
    /// <param name="requestBody"> </param>
    /// <param name="headers"> </param>
    /// <param name="contentType"> </param>
    /// <param name="jobId"> </param>
    /// <param name="jobKey"> </param>
    /// <param name="triggerId"> </param>
    /// <param name="triggerKey"> </param>
    /// <returns> </returns>
    internal static HttpRequestMessage BuildRequestMessage(
        string url,
        HttpRequestMethod method,
        string? requestBody,
        IReadOnlyDictionary<string, string>? headers,
        string contentType,
        long jobId,
        string jobKey,
        long? triggerId,
        string triggerKey)
    {
        var request = new HttpRequestMessage(ToHttpMethod(method), url);

        // 仅在存在请求体且方法允许携带 Body 时创建 Content
        if (!string.IsNullOrWhiteSpace(requestBody) &&
            (method == HttpRequestMethod.POST
                || method == HttpRequestMethod.PUT
                || method == HttpRequestMethod.PATCH))
        {
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = System.Net.Mime.MediaTypeNames.Application.Json;
            request.Content = new StringContent(requestBody, Encoding.UTF8, contentType);
        }

        if (headers != null)
        {
            foreach (var header in headers)
            {
                // 先尝试写入普通请求头；如果该头属于内容头，则回退写入 Content.Headers
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value) && request.Content != null)
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        // 追加调度上下文，方便下游服务定位本次调用对应的 Job/Trigger
        request.Headers.TryAddWithoutValidation("X-Quartz-Job-Id", jobId.ToString());
        request.Headers.TryAddWithoutValidation("X-Quartz-Job", jobKey);
        request.Headers.TryAddWithoutValidation("X-Quartz-Trigger-Id", triggerId?.ToString() ?? "-");
        request.Headers.TryAddWithoutValidation("X-Quartz-Trigger", triggerKey);

        return request;
    }
}