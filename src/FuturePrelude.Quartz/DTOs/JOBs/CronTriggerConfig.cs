namespace FuturePrelude.Quartz;

/// <summary> CronTrigger 专属配置 </summary>
public sealed class CronTriggerConfig
{
    /// <summary> Cron 表达式 </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary> Cron 描述 </summary>
    public string? CronDescription { get; set; }
}