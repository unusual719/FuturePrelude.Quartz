namespace FuturePrelude.Quartz.Core;

/// <summary> 处理时间格式，DateTime 转 string </summary>
public class DateOnlySchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(DateOnly) && schema is OpenApiSchema openApiSchema)
        {
            openApiSchema.Format = "date";
            openApiSchema.Type = JsonSchemaType.String;
        }
    }
}