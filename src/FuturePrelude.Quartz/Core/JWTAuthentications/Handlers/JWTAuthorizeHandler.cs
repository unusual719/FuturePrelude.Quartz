namespace FuturePrelude.Quartz.Core;

/// <summary> JWT 授权处理器 </summary>
/// <remarks> 负责处理基于 JWT Token 的授权验证逻辑 </remarks>
public class JWTAuthorizeHandler : IAuthorizationHandler
{
    private readonly ILogger<JWTAuthorizeHandler> _logger;

    /// <summary> 初始化 JWT 授权处理器 </summary>
    /// <param name="logger"> </param>
    public JWTAuthorizeHandler(ILogger<JWTAuthorizeHandler> logger)
    {
        _logger = logger;
    }

    /// <summary> 异步处理授权验证 </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        // 获取当前 HTTP 上下文
        var httpContext = context.GetCurrentHttpContext();

        // 检查 endpoint 是否标记了 [AllowAnonymous] 特性
        if (httpContext.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            // 标记了 [AllowAnonymous] 的端点允许匿名访问，跳过授权处理
            return Task.CompletedTask;
        }

        // 从请求中提取 JWT Token
        var token = httpContext.GetJwtBearerToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("JWT Token 为空，拒绝访问.");
            context.Fail();
            return Task.CompletedTask;
        }

        // 检查用户是否已通过身份认证
        var isAuthenticated = context.User.Identity.IsAuthenticated;
        if (!isAuthenticated)
        {
            _logger.LogWarning("用户未通过身份认证，拒绝访问.");
            context.Fail();
            return Task.CompletedTask;
        }

        // 处理待定的授权需求
        var pendingRequirements = context.PendingRequirements.ToList();
        foreach (var requirement in pendingRequirements)
        {
            // JWT 默认会处理 DenyAnonymousAuthorizationRequirement
            // 参考: https://learn.microsoft.com/zh-cn/aspnet/core/security/authorization/policies
            if (requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement)
            {
                // 用户已认证，满足该需求
                context.Succeed(requirement);
            }
            else
            {
                // 可在此扩展其他自定义授权需求处理 实现方式：创建继承自 IAuthorizationRequirement 的需求类
            }
        }

        return Task.CompletedTask;
    }
}
