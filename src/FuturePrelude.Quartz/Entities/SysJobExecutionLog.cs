using System.ComponentModel.DataAnnotations;

namespace FuturePrelude.Quartz;

/// <summary> 任务执行日志 </summary>
[Table(Name = "SysJobExecutionLog")]
[Index("idx_jobid_createtime", nameof(JobId) + "," + nameof(CreateTime) + " desc")]
[Index("idx_triggerid_createtime", nameof(TriggerId) + "," + nameof(CreateTime) + " desc")]
[Index("idx_starttime", nameof(StartTimeUtc))]
[Index("idx_endtime", nameof(EndTimeUtc))]
[Index("idx_jobid_starttime", nameof(JobId) + "," + nameof(StartTimeUtc) + " desc")]
[Index("idx_triggerid_starttime", nameof(TriggerId) + "," + nameof(StartTimeUtc) + " desc")]
public class SysJobExecutionLog
{
    /// <summary> Id 主键 </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary> 任务ID（SysJobDetail.Id） </summary>
    [Column(IsNullable = true)]
    public long? JobId { get; set; }

    /// <summary> 触发器ID（SysJobTrigger.Id） </summary>
    [Column(IsNullable = true)]
    public long? TriggerId { get; set; }

    /// <summary> 任务类型快照 </summary>
    [Column(IsNullable = true)]
    public JobType? JobType { get; set; }

    /// <summary> JobKey </summary>
    [Column(IsNullable = true)]
    public string JobKey { get; set; }

    /// <summary> TriggerKey </summary>
    [Column(IsNullable = true)]
    public string TriggerKey { get; set; }

    /// <summary> 任务名称快照 </summary>
    [Column(IsNullable = true)]
    public string JobNameSnapshot { get; set; }

    /// <summary> 触发器名称快照 </summary>
    [Column(IsNullable = true)]
    public string TriggerNameSnapshot { get; set; }

    /// <summary> 单次运行标识 </summary>
    [Column(IsNullable = false)]
    public string RunId { get; set; }

    /// <summary> 同一条执行链根运行标识 </summary>
    [Column(IsNullable = true)]
    public string RootRunId { get; set; }

    /// <summary> 第几次尝试（首次为 1） </summary>
    [Column(IsNullable = false)]
    public int AttemptNo { get; set; } = 1;

    /// <summary> 执行来源（调度触发/手动触发/失败重试/恢复补偿） </summary>
    [Column(IsNullable = true)]
    public string ExecutionSource { get; set; }

    /// <summary> Quartz 触发实例标识 </summary>
    [Column(IsNullable = true)]
    public string FireInstanceId { get; set; }

    /// <summary> 调度计划触发时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? ScheduledFireTimeUtc { get; set; }

    /// <summary> 开始时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? StartTimeUtc { get; set; }

    /// <summary> 结束时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? EndTimeUtc { get; set; }

    /// <summary> 耗时（毫秒） </summary>
    [Column(IsNullable = true)]
    public double? DurationMs { get; set; }

    /// <summary> 执行结果 </summary>
    [Column(IsNullable = false)]
    [MaxLength(-1)]
    public string Result { get; set; }

    /// <summary> 执行实例节点 </summary>
    [Column(IsNullable = true)]
    public string ExecutorNode { get; set; }

    /// <summary> 结果码（如 Timeout/LockNotAcquired/ConcurrentBlocked） </summary>
    [Column(IsNullable = true)]
    public string ReasonCode { get; set; }

    /// <summary> 结果说明 </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string ReasonMessage { get; set; }

    /// <summary> 异常类型 </summary>
    [Column(IsNullable = true)]
    public string ExceptionType { get; set; }

    /// <summary> 异常消息摘要 </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string Exception { get; set; }

    /// <summary> 异常堆栈摘要 </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string StackTrace { get; set; }

    /// <summary> 返回值/响应摘要 </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string ReturnValue { get; set; }

    /// <summary> 执行上下文快照（JSON） </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string ExecutionContextJson { get; set; }

    /// <summary> 执行摘要 </summary>
    [Column(IsNullable = true)]
    [MaxLength(-1)]
    public string ExecutionSummary { get; set; }

    /// <summary> 任务创建人 </summary>
    [Column(IsNullable = true)]
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    [Column(IsNullable = false)]
    public DateTime CreateTime { get; set; } = ChinaTimeZoneConverter.Now();
}
