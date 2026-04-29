using Newtonsoft.Json.Linq;

namespace FuturePrelude.Quartz;

/// <summary>
/// Bearer Token 运行时配置
/// <para> 用于承接前端在 <see cref="SysJobHttpConfig.AuthCredentials" /> 中保存的 JSON 内容， 方便后续在任务执行阶段解析“固定 Token”或“先请求获取 Token”两种模式 </para>
/// </summary>
public class HttpBearerTokenRuntimeConfig
{
    /// <summary> 是否需要在真正发起业务请求前，先额外调用一次认证接口获取 Token `false` 表示直接使用 <see cref="Token" />； `true` 表示需要结合 <see cref="TokenRequestUrl" />、 <see cref="TokenRequestMethod" /> 等信息先取 Token </summary>
    public bool NeedRequestToken { get; set; } = false;

    /// <summary> 固定 Bearer Token </summary>
    public string? Token { get; set; }

    /// <summary> 获取 Token 的认证接口地址 当 <see cref="NeedRequestToken" /> 为 <c> true </c> 时，应提供一个绝对 URL </summary>
    public string? TokenRequestUrl { get; set; }

    /// <summary>
    /// 获取 Token 的请求方法
    /// <para> 当前默认使用 `POST`，但运行时仍保留字符串形式，方便后续扩展为 `GET/POST/PUT/PATCH/DELETE` </para>
    /// </summary>
    public string TokenRequestMethod { get; set; } = "POST";

    /// <summary> 获取 Token 接口的请求头集合 </summary>
    public Dictionary<string, string>? TokenRequestHeaders { get; set; }

    /// <summary> 获取 Token 接口的请求体 使用 <see cref="JToken" /> 承接任意 JSON 结构 </summary>
    public JToken? TokenRequestBody { get; set; }

    /// <summary>
    /// 从认证接口响应 JSON 中提取 Token 的路径
    /// <para> 例如：`data.access_token` 该字段后续通常会结合 <see cref="JToken.SelectToken(string)" /> 使用 </para>
    /// </summary>
    public string? TokenPath { get; set; }

    /// <summary> 最终业务请求写入认证头时使用的 Header Key 默认值为 `Authorization` </summary>
    public string HeaderKey { get; set; } = "Authorization";

    /// <summary> 请求写入认证头时使用的前缀，默认值为 `Bearer` </summary>
    public string Prefix { get; set; } = "Bearer";

    public void Validate()
    {
        // 是否需要获取 Token
        if (!NeedRequestToken)
        {
            // 不需要请求 Token → 必须提供固定 Token
            if (string.IsNullOrWhiteSpace(Token))
                throw new InvalidOperationException("未启用 Token 请求模式时，必须提供固定 Token");

            return;
        }

        // 需要请求 Token → 校验请求地址
        if (string.IsNullOrWhiteSpace(TokenRequestUrl))
            throw new InvalidOperationException("启用 Token 请求模式时，必须配置 TokenRequestUrl");
        if (!Uri.TryCreate(TokenRequestUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException($"TokenRequestUrl 非法：{TokenRequestUrl}");

        // 请求方法校验
        if (string.IsNullOrWhiteSpace(TokenRequestMethod))
            throw new InvalidOperationException("TokenRequestMethod 不能为空");
        HttpBearerTokenRuntime.ToHttpMethod(TokenRequestMethod);

        // NeedRequestToken Token 提取路径
        if (string.IsNullOrWhiteSpace(TokenPath))
            throw new InvalidOperationException("启用 Token 请求模式时，必须配置 TokenPath");

        // HeaderKey
        if (string.IsNullOrWhiteSpace(HeaderKey)) HeaderKey = "Authorization";

        // Prefix
        if (string.IsNullOrWhiteSpace(Prefix)) Prefix = "Bearer";
    }
}
