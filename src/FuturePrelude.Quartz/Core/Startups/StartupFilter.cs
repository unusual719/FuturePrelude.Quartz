namespace FuturePrelude.Quartz.Core;

/// <summary> 应用启动时自动注册中间件 </summary>
/// <remarks> </remarks>
public class StartupFilter : IStartupFilter
{
    /// <summary> 解析方法参数实例 </summary>
    /// <param name="app"> </param>
    /// <param name="method"> </param>
    /// <returns> </returns>
    private static object[] ResolveMethodParameterInstances(IApplicationBuilder app, MethodInfo method)
    {
        // 获取方法所有参数
        var parameters = method.GetParameters();
        var parameterInstances = new object[parameters.Length];
        parameterInstances[0] = app;

        // 解析服务
        for (var i = 1; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            parameterInstances[i] = app.ApplicationServices.GetRequiredService(parameter.ParameterType);
        }

        return parameterInstances;
    }

    /// <summary> 配置中间件 </summary>
    /// <param name="next"> </param>
    /// <returns> </returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // 存储根服务
            App.ServiceProvider ??= app.ApplicationServices;

            // 环境名
            var envName = App.HostEnvironment?.EnvironmentName ?? "Unknown";
            var version = $"{GetType().Assembly.GetName().Version}";

            // 设置响应报文头信息
            app.Use(async (context, next) =>
            {
                // 处理 WebSocket 请求
                if (context.IsWebSocketRequest()) await next.Invoke();
                else
                {
                    // 输出当前环境标识
                    context.Response.Headers["Environment"] = envName;

                    // 输出框架版本
                    context.Response.Headers[nameof(FuturePrelude.Quartz)] = version;

                    // 执行下一个中间件
                    await next.Invoke();

                    // 解决刷新 Token 时间和 Token 时间相近问题
                    if (!context.Response.HasStarted
                        && context.Response.StatusCode == StatusCodes.Status401Unauthorized
                        && context.Response.Headers.ContainsKey("access-token")
                        && context.Response.Headers.ContainsKey("x-access-token"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    }
                }
            });

            // 调用启动层的 Startup
            next(app);
        };
    }
}