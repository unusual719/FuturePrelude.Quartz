namespace FuturePrelude.Quartz;

/// <summary>
/// 标记此特性可跳过响应包装处理
/// <para> 当应用于控制器或 Action 方法时，响应不会被 <see cref="ResponseWrapperMiddleware" /> 包装成标准 <see cref="ApiResponse" /><c> &lt;T&gt; </c> 格式 </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipResponseWrapAttribute : Attribute
{
}