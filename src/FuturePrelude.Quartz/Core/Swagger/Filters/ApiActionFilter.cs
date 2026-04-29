namespace FuturePrelude.Quartz.Core;

/// <summary> 处理注释及接口描述 </summary>
public class ApiActionFilter : IOperationFilter
{
    /// <summary> 实现过滤器方法 </summary>
    /// <param name="operation"> </param>
    /// <param name="context"> </param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // 获取方法
        var method = context.MethodInfo;

        // 处理定义 [DisplayName] 特性但并未注释的情况
        if (string.IsNullOrWhiteSpace(operation.Summary) && method.IsDefined(typeof(DisplayNameAttribute), true))
        {
            var displayName = method.GetCustomAttribute<DisplayNameAttribute>(true);
            if (!string.IsNullOrWhiteSpace(displayName.DisplayName))
            {
                operation.Summary = displayName.DisplayName;
            }
        }

        // 处理过时
        if (method.IsDefined(typeof(ObsoleteAttribute), true))
        {
            var deprecated = method.GetCustomAttribute<ObsoleteAttribute>(true);
            if (!string.IsNullOrWhiteSpace(deprecated.Message))
            {
                operation.Description = $"<div>{deprecated.Message}</div>" + operation.Description;
            }
        }
    }
}
