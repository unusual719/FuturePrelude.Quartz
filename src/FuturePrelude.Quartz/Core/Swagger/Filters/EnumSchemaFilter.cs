namespace FuturePrelude.Quartz.Core;

/// <summary> Enum 提示 </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    /// <summary> 中文正则表达式 </summary>
    private const string CHINESE_PATTERN = @"[\u4e00-\u9fa5]";

    /// <summary> 实现过滤器方法 </summary>
    /// <param name="model"> </param>
    /// <param name="context"> </param>
    public void Apply(IOpenApiSchema model, SchemaFilterContext context)
    {
        var type = context.Type;

        // 排除其他程序集的枚举
        if (type.IsEnum && type.Assembly == typeof(EnumSchemaFilter).Assembly && model is OpenApiSchema openApiSchema)
        {
            openApiSchema.Enum ??= [];
            openApiSchema.Enum.Clear();
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{model.Description}<br />");

            var enumValues = Enum.GetValues(type);

            bool convertToNumber = true;

            // 获取枚举实际值类型
            var enumValueType = type.GetField("value__")?.FieldType;
            if (enumValueType == null)
            {
                return;
            }

            foreach (var value in enumValues)
            {
                var numValue = Convert.ChangeType(value, enumValueType);

                // 获取枚举成员特性
                var fieldName = Enum.GetName(type, value);
                var fieldInfo = fieldName == null ? null : type.GetField(fieldName);
                var descriptionAttribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>(true);

                openApiSchema.Enum.Add(JsonValue.Create(value.ToString())!);

                stringBuilder.Append($"&nbsp;{descriptionAttribute?.Description} {value} = {numValue}<br />");
            }

            model.Description = stringBuilder.ToString();

            if (!convertToNumber)
            {
                openApiSchema.Type = JsonSchemaType.String;
                openApiSchema.Format = null;
            }
        }
    }
}
