namespace FuturePrelude.Quartz;

/// <summary> 任务运行状态枚举 </summary>
public enum RunStatus
{
    /// <summary> 等待执行 </summary>
    [Description("等待执行")]
    Pending = 0,

    /// <summary> 执行成功 </summary>
    [Description("成功")]
    Success = 1,

    /// <summary> 执行失败 </summary>
    [Description("失败")]
    Failed = 2,

    /// <summary> 执行超时 </summary>
    [Description("超时")]
    Timeout = 3,

    /// <summary> 已取消 </summary>
    [Description("取消")]
    Cancelled = 4,

    /// <summary> 重试中 </summary>
    [Description("重试中")]
    Retrying = 5
}