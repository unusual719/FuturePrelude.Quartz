namespace FuturePrelude.Quartz;

/// <summary> <see cref="HttpContext" /> 拓展类 </summary>
public static class HttpContextExtensions
{
    /// <summary> 设置规范化文档退出登录 </summary>
    /// <param name="httpContext"> </param>
    public static void SignoutToSwagger(this HttpContext httpContext)
    {
        httpContext.Response.Headers["access-token"] = "invalid_token";
    }

    /// <summary> 设置响应头 Tokens </summary>
    /// <param name="httpContext"> </param>
    /// <param name="accessToken"> </param>
    /// <param name="refreshToken"> </param>
    public static void SetTokensOfResponseHeaders(this HttpContext httpContext, string accessToken, string refreshToken = null)
    {
        httpContext.Response.Headers["access-token"] = accessToken;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            httpContext.Response.Headers["x-access-token"] = refreshToken;
        }
    }

    /// <summary> 获取当前 HttpContext 上下文 </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    public static DefaultHttpContext GetCurrentHttpContext(this AuthorizationHandlerContext context)
    {
        DefaultHttpContext httpContext;

        // 获取 httpContext 对象
        if (context.Resource is AuthorizationFilterContext filterContext) httpContext = (DefaultHttpContext)filterContext.HttpContext;
        else if (context.Resource is DefaultHttpContext defaultHttpContext) httpContext = defaultHttpContext;
        else httpContext = null;

        return httpContext;
    }

    /// <summary> 获取完整的请求 URL 地址 </summary>
    /// <param name="httpRequest"> <see cref="HttpRequest" /> </param>
    /// <returns> <see cref="string" /> </returns>
    public static string GetFullRequestUrl(this HttpRequest httpRequest) =>
        new StringBuilder()
            .Append(httpRequest.Scheme)
            .Append("://")
            .Append(httpRequest.Host.Value)
            .Append(httpRequest.PathBase)
            .Append(httpRequest.Path)
            .Append(httpRequest.QueryString)
            .ToString();

    /// <summary> 判断是否是 WebSocket 请求 </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    public static bool IsWebSocketRequest(this HttpContext context) => context.WebSockets.IsWebSocketRequest || context.Request.Path == "/ws";

    /// <summary> 获取 JWT Bearer Token </summary>
    /// <param name="httpContext"> </param>
    /// <param name="headerKey"> </param>
    /// <param name="tokenPrefix"> </param>
    /// <returns> </returns>
    public static string GetJwtBearerToken(this HttpContext httpContext, string headerKey = "Authorization"
        , string tokenPrefix = "Bearer ")
    {
        // 判断请求报文头中是否有 "Authorization" 报文头
        var bearerToken = httpContext.Request.Headers[headerKey].ToString();
        if (string.IsNullOrWhiteSpace(bearerToken)) return default;

        if (!bearerToken.StartsWith(tokenPrefix))
            return tokenPrefix;

        var prefixLenght = tokenPrefix.Length;
        return bearerToken.StartsWith(tokenPrefix, true, null) && bearerToken.Length > prefixLenght ? bearerToken[prefixLenght..] : default;
    }
}