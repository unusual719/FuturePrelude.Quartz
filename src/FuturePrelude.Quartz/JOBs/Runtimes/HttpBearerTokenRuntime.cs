using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace FuturePrelude.Quartz;

/// <summary> Bearer Token 运行时辅助类 </summary>
internal static class HttpBearerTokenRuntime
{
    /// <summary> 将原始 Token 值拼装为最终请求头值 </summary>
    /// <param name="config"> </param>
    /// <param name="accessToken"> </param>
    /// <returns> </returns>
    private static string BuildAuthorizationHeaderValue(HttpBearerTokenRuntimeConfig config, string accessToken)
    {
        ArgumentNullException.ThrowIfNull(config);

        var token = accessToken?.Trim() ?? string.Empty;
        var prefix = config.Prefix?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(prefix) ? token : $"{prefix} {token}";
    }

    /// <summary> 将字符串形式的请求方式转换为 <see cref="HttpMethod" /> </summary>
    /// <param name="method"> </param>
    /// <returns> 转换后的标准 <see cref="HttpMethod" /> 对象 </returns>
    internal static HttpMethod ToHttpMethod(string? method)
    {
        return (method ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            _ => throw new NotSupportedException($"不支持的 Token 请求方式: {method}")
        };
    }

    /// <summary> 反序列化 Bearer 认证配置 JSON，并补齐运行时默认值 </summary>
    /// <param name="authCredentialsJson"> 来自 <see cref="SysJobHttpConfig.AuthCredentials" /> 的 JSON 文本 允许为空；为空时返回 <c> null </c> </param>
    /// <returns> 解析成功后返回 <see cref="HttpBearerTokenRuntimeConfig" />； 当原始配置为空白时返回 <c> null </c> </returns>
    /// <exception cref="JsonException"> 当 JSON 结构非法时抛出 </exception>
    internal static HttpBearerTokenRuntimeConfig? DeserializeConfig(string? authCredentialsJson)
    {
        if (string.IsNullOrWhiteSpace(authCredentialsJson))
        {
            return null;
        }

        var config = JsonConvert.DeserializeObject<HttpBearerTokenRuntimeConfig>(authCredentialsJson.Trim())
                     ?? new HttpBearerTokenRuntimeConfig();

        if (string.IsNullOrWhiteSpace(config.TokenRequestMethod))
        {
            config.TokenRequestMethod = "POST";
        }

        if (string.IsNullOrWhiteSpace(config.HeaderKey))
        {
            config.HeaderKey = "Authorization";
        }

        if (string.IsNullOrWhiteSpace(config.Prefix))
        {
            config.Prefix = "Bearer";
        }

        return config;
    }

    /// <summary> 根据运行时配置构造 “获取 Token” 用的 HTTP 请求对象 该方法仅负责构建请求 </summary>
    /// <param name="config"> </param>
    /// <returns> </returns>
    /// <exception cref="InvalidOperationException"> </exception>
    internal static HttpRequestMessage BuildTokenRequestMessage(HttpBearerTokenRuntimeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.TokenRequestUrl))
        {
            throw new InvalidOperationException("未配置 Token 请求地址，无法构造认证请求");
        }

        var request = new HttpRequestMessage(ToHttpMethod(config.TokenRequestMethod), config.TokenRequestUrl);

        if (config.TokenRequestBody != null &&
            request.Method != HttpMethod.Get &&
            request.Method != HttpMethod.Delete)
        {
            request.Content = new StringContent(
                config.TokenRequestBody.ToString(Formatting.None),
                System.Text.Encoding.UTF8,
                MediaTypeNames.Application.Json);
        }

        if (config.TokenRequestHeaders != null)
        {
            foreach (var header in config.TokenRequestHeaders)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value) && request.Content != null)
                {
                    request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        return request;
    }

    /// <summary> 从认证接口返回的 JSON 文本中，根据指定路径提取 Token 字符串 </summary>
    /// <param name="responseContent"> </param>
    /// <param name="tokenPath"> </param>
    /// <returns> </returns>
    internal static string? ExtractToken(string? responseContent, string? tokenPath)
    {
        if (string.IsNullOrWhiteSpace(responseContent) || string.IsNullOrWhiteSpace(tokenPath))
        {
            return null;
        }

        var json = JToken.Parse(responseContent);
        var token = json.SelectToken(tokenPath);

        return token?.Type switch
        {
            null => null,
            JTokenType.Null => null,
            JTokenType.String => token.Value<string>(),
            _ => token.ToString(Formatting.None)
        };
    }

    /// <summary> 将 Bearer Token 应用到请求头集合中 </summary>
    /// <param name="headers"> </param>
    /// <param name="config"> </param>
    /// <param name="accessToken"> </param>
    internal static void ApplyAuthorizationHeader(
        HttpRequestHeaders headers,
        HttpBearerTokenRuntimeConfig config,
        string accessToken)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(config);

        var headerKey = string.IsNullOrWhiteSpace(config.HeaderKey)
            ? "Authorization"
            : config.HeaderKey.Trim();
        var headerValue = BuildAuthorizationHeaderValue(config, accessToken);

        headers.Remove(headerKey);
        headers.TryAddWithoutValidation(headerKey, headerValue);
    }

    /// <summary> 统一处理 Bearer Token 逻辑（获取 + 应用） </summary>
    /// <param name="httpClientFactory"> </param>
    /// <param name="request"> </param>
    /// <param name="authCredentialsJson"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    /// <exception cref="InvalidOperationException"> </exception>
    internal static async Task<bool> ApplyBearerTokenAsync(
        IHttpClientFactory httpClientFactory,
        HttpRequestMessage request,
        string? authCredentialsJson,
        CancellationToken cancellationToken = default)
    {
        // 解析配置
        var config = DeserializeConfig(authCredentialsJson);
        if (config == null)
        {
            return false; // 未启用认证
        }

        // 如果有静态 Token（优先）
        if (!string.IsNullOrWhiteSpace(config.Token))
        {
            ApplyAuthorizationHeader(request.Headers, config, config.Token);
            return true;
        }

        // 是否需要动态获取 Token
        if (string.IsNullOrWhiteSpace(config.TokenRequestUrl))
        {
            throw new InvalidOperationException("未配置 Token 或 Token 请求地址");
        }

        // 构建 Token 请求
        var httpClient = httpClientFactory.CreateClient();
        using var tokenRequest = BuildTokenRequestMessage(config);
        using var response = await httpClient.SendAsync(tokenRequest, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"获取 Token 失败，状态码: {(int)response.StatusCode}，响应: {content}");
        }

        // 提取 Token
        var accessToken = ExtractToken(content, config.TokenPath);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("未能从响应中提取 Token，请检查 TokenPath 配置");
        }

        // 应用到请求头
        ApplyAuthorizationHeader(request.Headers, config, accessToken);

        return true;
    }
}
