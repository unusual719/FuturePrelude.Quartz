namespace FuturePrelude.Quartz.Core;

/// <summary> object schema，统一显示为 any </summary>
public class AnySchemaFilter : ISchemaFilter
{
    // 相关 issue：https://github.com/swagger-api/swagger-codegen-generators/issues/692

    /// <summary> 实现过滤器方法 </summary>
    /// <param name="model"> </param>
    /// <param name="context"> </param>
    public void Apply(IOpenApiSchema model, SchemaFilterContext context)
    {
        var type = context.Type;

        if (type == typeof(object) && model is OpenApiSchema schema)
        {
            schema.AdditionalPropertiesAllowed = false;
        }
    }
}