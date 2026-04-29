namespace FuturePrelude.Quartz;

/// <summary> 触发器入参 </summary>
public class TriggerInput
{
    /// <summary> 触发器ID（更新时必填） </summary>
    public long Id { get; set; }

    /// <summary> 触发器键（Group:Name 格式） </summary>
    public string TriggerKey { get; set; }

    /// <summary> 触发器名称 </summary>
    public string TriggerName { get; set; }

    /// <summary> 触发器分组 </summary>
    public string TriggerGroup { get; set; }

    /// <summary> 触发器类型（0=Cron, 1=Simple, 2=Calendar, 3=Daily） </summary>
    public int TriggerType { get; set; }

    /// <summary> Cron 表达式（TriggerType=0 时必填） </summary>
    public string CronExpression { get; set; }

    /// <summary> Cron 描述（可选，用于展示） </summary>
    public string? CronDescription { get; set; }

    /// <summary> 时区ID（可选，默认使用中国时区） </summary>
    public string? TimeZoneId { get; set; }

    /// <summary> 触发器生效开始时间（UTC） </summary>
    public DateTime? StartTimeUtc { get; set; }

    /// <summary> 触发器生效结束时间（UTC） </summary>
    public DateTime? EndTimeUtc { get; set; }

    /// <summary> Misfire 策略（0=不处理, 1=DoNothing, 2=FireOnceNow） </summary>
    public int MisfireStrategy { get; set; } = 0;

    /// <summary> 优先级（默认5） </summary>
    public int Priority { get; set; } = 5;

    /// <summary> 状态（0=禁用, 1=启用） </summary>
    public int Status { get; set; }

    /// <summary> 触发器描述 </summary>
    public string Description { get; set; }
}