namespace FuturePrelude.Quartz;

/// <summary> Quartz 触发器类型 </summary>
public enum TriggerType
{
    /// <summary> Cron 表达式 </summary>
    [Description("Cron 触发器")]
    Cron,

    /// <summary> 简单（秒，分，时，天） </summary>
    [Description("简单触发器")]
    Simple,

    /// <summary> 每日 </summary>
    [Description("每日")]
    Daily
}