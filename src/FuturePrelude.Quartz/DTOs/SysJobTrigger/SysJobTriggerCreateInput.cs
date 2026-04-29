namespace FuturePrelude.Quartz;

/// <summary> 创建任务触发器入参 </summary>
public class SysJobTriggerCreateInput
{
    /// <summary> 任务ID </summary>
    public long JobId { get; set; }

    /// <summary> 触发器名称 </summary>
    public string TriggerName { get; set; } = string.Empty;

    /// <summary> 触发器分组，不传时默认使用任务分组 </summary>
    public string? TriggerGroup { get; set; }

    /// <summary> 触发器类型，当前仅支持 Cron </summary>
    public TriggerType TriggerType { get; set; } = TriggerType.Cron;

    /// <summary> Cron 表达式 </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary> Cron 描述 </summary>
    public string? CronDescription { get; set; }

    /// <summary> 时区ID </summary>
    public string? TimeZoneId { get; set; }

    /// <summary> 生效开始时间（UTC） </summary>
    public DateTime? StartTimeUtc { get; set; } = null;

    /// <summary> 生效结束时间（UTC） </summary>
    public DateTime? EndTimeUtc { get; set; } = null;

    /// <summary> Misfire 策略 </summary>
    public MisfireAction MisfireStrategy { get; set; } = MisfireAction.SmartPolicy;

    /// <summary> 优先级 </summary>
    public int Priority { get; set; } = 5;

    /// <summary> 启用状态 </summary>
    public EnableStatus EnableStatus { get; set; } = EnableStatus.Enabled;

    /// <summary> 触发器描述 </summary>
    public string? Description { get; set; }
}