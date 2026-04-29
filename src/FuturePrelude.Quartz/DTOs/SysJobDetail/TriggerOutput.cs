namespace FuturePrelude.Quartz;

/// <summary> 触发器出参 </summary>
public class TriggerOutput
{
    /// <summary> 主键ID </summary>
    public long Id { get; set; }

    /// <summary> 任务ID </summary>
    public long JobId { get; set; }

    /// <summary> TriggerKey </summary>
    public string TriggerKey { get; set; }

    /// <summary> 触发器名称 </summary>
    public string TriggerName { get; set; }

    /// <summary> 触发器分组 </summary>
    public string TriggerGroup { get; set; }

    /// <summary> 触发器类型 </summary>
    public TriggerType TriggerType { get; set; }

    /// <summary> Cron 表达式 </summary>
    public string? CronExpression { get; set; }

    /// <summary> Cron 描述 </summary>
    public string? CronDescription { get; set; }

    /// <summary> 时区ID </summary>
    public string TimeZoneId { get; set; }

    /// <summary> 错过触发处理策略 </summary>
    public MisfireAction MisfireStrategy { get; set; }

    /// <summary> 优先级 </summary>
    public int Priority { get; set; }

    /// <summary> 生效开始时间（UTC） </summary>
    public DateTime? StartTimeUtc { get; set; }

    /// <summary> 生效结束时间（UTC） </summary>
    public DateTime? EndTimeUtc { get; set; }

    /// <summary> 上一次运行时间（UTC） </summary>
    public DateTime? PrevFireTimeUtc { get; set; }

    /// <summary> 下一次触发时间（UTC） </summary>
    public DateTime? NextFireTimeUtc { get; set; }

    /// <summary> 启用状态 </summary>
    public EnableStatus EnableStatus { get; set; }

    /// <summary> 描述 </summary>
    public string Description { get; set; }

    /// <summary> 触发器状态枚举 </summary>
    public TriggerState TriggerState { get; set; }

    /// <summary> 触发器状态 </summary>
    public string TriggerStateName { get; set; } = string.Empty;
}
