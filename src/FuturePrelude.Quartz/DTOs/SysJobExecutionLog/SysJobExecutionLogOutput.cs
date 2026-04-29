namespace FuturePrelude.Quartz;

/// <summary> 任务执行记录出参 </summary>
public class SysJobExecutionLogOutput
{
    /// <summary> 主键ID </summary>
    public long Id { get; set; }

    /// <summary> 任务ID </summary>
    public long? JobId { get; set; }

    /// <summary> 触发器ID </summary>
    public long? TriggerId { get; set; }

    /// <summary> 任务类型 </summary>
    public JobType? JobType { get; set; }

    /// <summary> JobKey </summary>
    public string? JobKey { get; set; }

    /// <summary> TriggerKey </summary>
    public string? TriggerKey { get; set; }

    /// <summary> 任务名称快照 </summary>
    public string? JobNameSnapshot { get; set; }

    /// <summary> 触发器名称快照 </summary>
    public string? TriggerNameSnapshot { get; set; }

    /// <summary> 单次运行标识 </summary>
    public string? RunId { get; set; }

    /// <summary> 根运行标识 </summary>
    public string? RootRunId { get; set; }

    /// <summary> 第几次尝试 </summary>
    public int AttemptNo { get; set; }

    /// <summary> 执行来源 </summary>
    public string? ExecutionSource { get; set; }

    /// <summary> Quartz 触发实例标识 </summary>
    public string? FireInstanceId { get; set; }

    /// <summary> 调度计划触发时间（UTC） </summary>
    public DateTime? ScheduledFireTimeUtc { get; set; }

    /// <summary> 开始时间（UTC） </summary>
    public DateTime? StartTimeUtc { get; set; }

    /// <summary> 结束时间（UTC） </summary>
    public DateTime? EndTimeUtc { get; set; }

    /// <summary> 展示时间（优先结束时间，其次开始时间） </summary>
    public DateTime? DisplayTimeUtc { get; set; }

    /// <summary> 耗时（毫秒） </summary>
    public double? DurationMs { get; set; }

    /// <summary> 执行结果原始值 </summary>
    public string? Result { get; set; }

    /// <summary> 执行结果中文 </summary>
    public string? ResultText { get; set; }

    /// <summary> 是否执行中 </summary>
    public bool IsRunning { get; set; }

    /// <summary> 执行实例节点 </summary>
    public string? ExecutorNode { get; set; }

    /// <summary> 结果码 </summary>
    public string? ReasonCode { get; set; }

    /// <summary> 结果说明 </summary>
    public string? ReasonMessage { get; set; }

    /// <summary> 异常类型 </summary>
    public string? ExceptionType { get; set; }

    /// <summary> 异常消息摘要 </summary>
    public string? Exception { get; set; }

    /// <summary> 异常堆栈摘要 </summary>
    public string? StackTrace { get; set; }

    /// <summary> 返回值/响应摘要 </summary>
    public string? ReturnValue { get; set; }

    /// <summary> 结果展示摘要 </summary>
    public string? DisplayMessage { get; set; }

    /// <summary> 执行上下文快照 </summary>
    public string? ExecutionContextJson { get; set; }

    /// <summary> 执行摘要 </summary>
    public string ExecutionSummary { get; set; }

    /// <summary> 创建人 </summary>
    public string? CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    public DateTime CreateTime { get; set; }
}
