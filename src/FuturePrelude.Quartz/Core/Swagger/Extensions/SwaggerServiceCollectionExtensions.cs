namespace FuturePrelude.Quartz.Core;

public static class SwaggerServiceCollectionExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(swaggerGenOptions =>
        {
            // 配置版本
            swaggerGenOptions.SwaggerDoc(
                "Default",
                new OpenApiInfo
                {
                    Title = "FuturePrelude.Quartz",
                    Version = "V1.0",
                    Description = string.Empty,
                    License = new OpenApiLicense { Name = string.Empty }
                }
            );

            // 加载 XML 文件
            InternalUtility.LoadXmlComments(swaggerGenOptions);

            // 认证授权
            swaggerGenOptions.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Description = "JWT Authorization header using the Bearer scheme.",
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                }
            );

            swaggerGenOptions.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document, null!),
                        []
                    }
                }
            );

            // 设置枚举类型显示
            swaggerGenOptions.SchemaFilter<EnumSchemaFilter>();

            // 设置 DateOnly 类型显示
            swaggerGenOptions.SchemaFilter<DateOnlySchemaFilter>();

            // C# object 类型问题
            swaggerGenOptions.SchemaFilter<AnySchemaFilter>();

            // 添加 Action 操作过滤器
            swaggerGenOptions.OperationFilter<ApiActionFilter>();

            // 启用对非空引用类型的支持
            swaggerGenOptions.SupportNonNullableReferenceTypes();

            // 所有参数以CamelCase风格命名
            swaggerGenOptions.DescribeAllParametersInCamelCase();
        });

        return services;
    }
}