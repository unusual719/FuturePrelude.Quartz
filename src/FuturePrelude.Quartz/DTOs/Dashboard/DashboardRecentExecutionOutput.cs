namespace FuturePrelude.Quartz;

/// <summary> 表示最近任务执行记录信息 </summary>
public sealed class DashboardRecentExecutionOutput
{
    /// <summary> 执行记录唯一标识 </summary>
    public long Id { get; set; }

    /// <summary> 任务名称 </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary> 任务所属组 </summary>
    public string TaskGroup { get; set; } = string.Empty;

    /// <summary> 执行时间 </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary> 执行状态代码（如：0=成功，1=失败） </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary> 执行状态文本描述（如：成功、失败、超时） </summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary> 执行耗时（毫秒） </summary>
    public double? DurationMs { get; set; }

    /// <summary> 执行结果消息或错误信息 </summary>
    public string Message { get; set; } = string.Empty;
}