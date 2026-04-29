namespace FuturePrelude.Quartz.Core;

/// <summary> JWT 服务注入拓展静态类 </summary>
public static class JWTServiceCollectionExtensions
{
    /// <summary> 添加 JWT 授权 </summary>
    /// <param name="services"> </param>
    /// <returns> </returns>
    public static AuthenticationBuilder AddJwt(this IServiceCollection services)
    {
        var jwtSettings = App.GetOptions<JWTOptions>();

        // 添加默认授权
        var authenticationBuilder = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        });

        // 添加授权
        authenticationBuilder.AddJwtBearer(options =>
        {
            // 配置 JWT 验证信息
            options.TokenValidationParameters = JWTEncryption.CreateValidationParameters(jwtSettings);
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.HttpContext.Request.Path.StartsWithSegments("/hubs/dashboard"))
                    {
                        var accessToken = context.Request.Query["access_token"].ToString();
                        if (!string.IsNullOrWhiteSpace(accessToken))
                        {
                            context.Token = accessToken;
                        }
                    }

                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();

                    if (context.Response.HasStarted)
                    {
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new ApiResponse
                    {
                        Code = StatusCodes.Status401Unauthorized,
                        Data = null,
                        Message = "认证失败或登录已过期，请重新登录",
                        TraceId = context.HttpContext.TraceIdentifier
                    });
                }
            };
        });

        // 启用全局授权
        authenticationBuilder.Services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new AuthorizeFilter());
        });

        return authenticationBuilder;
    }
}
