namespace FuturePrelude.Quartz.Core;

public static class SwaggerApplicationBuilderExtensions
{
    private static void AddDefaultInterceptor(SwaggerUIOptions swaggerUIOptions)
    {
        // 自动登录token
        swaggerUIOptions.UseRequestInterceptor("function(request) { return defaultRequestInterceptor(request); }");
        swaggerUIOptions.UseResponseInterceptor("function(response) { return defaultResponseInterceptor(response); }");
    }

    private static void CustomizeIndex(SwaggerUIOptions swaggerUIOptions)
    {
        // swagger token 自动登录
        var thisAssembly = Assembly.GetExecutingAssembly();
        var customIndex = $"FuturePrelude.Quartz.Core.Swagger.Assets.index.html";
        swaggerUIOptions.IndexStream = () =>
        {
            StringBuilder htmlBuilder;

            // 读取文件内容
            using (var stream = thisAssembly.GetManifestResourceStream(customIndex))
            {
                using var reader = new StreamReader(stream);
                htmlBuilder = new StringBuilder(reader.ReadToEnd());
            }

            // 返回新的内存流
            var byteArray = Encoding.UTF8.GetBytes(htmlBuilder.ToString());
            return new MemoryStream(byteArray);
        };

        // 添加登录信息
        swaggerUIOptions.ConfigObject.AdditionalItems.Add(nameof(SwaggerLoginInfo), new JsonObject()
        {
            [nameof(SwaggerLoginInfo.Enabled)] = true,
            [nameof(SwaggerLoginInfo.CheckUrl)] = "/api/swagger/checkUrl",
            [nameof(SwaggerLoginInfo.SubmitUrl)] = "/api/swagger/submitUrl"
        });
    }

    public static IApplicationBuilder UseSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger(swaggerOptions =>
        {
            // 启动服务器 Servers
            swaggerOptions.PreSerializeFilters.Add((swagger, request) =>
            {
                var servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = $"{request.Scheme}://{request.Host.Value}" ,Description="Default" }
                };

                swagger.Servers = servers;
            });
        });

        app.UseSwaggerUI(swaggerUIOptions =>
        {
            // 自定义 Swagger 首页
            CustomizeIndex(swaggerUIOptions);

            // 配置多语言和自动登录token
            AddDefaultInterceptor(swaggerUIOptions);

            // swagger.json
            var endpoint = $"/swagger/Default/swagger.json";
            swaggerUIOptions.SwaggerEndpoint(endpoint, $"FlowBot.WebApi-V1");

            // 文档展开设置
            swaggerUIOptions.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        });

        return app;
    }
}
