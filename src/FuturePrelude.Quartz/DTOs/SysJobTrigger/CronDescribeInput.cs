namespace FuturePrelude.Quartz;

/// <summary> Cron 表达式描述查询入参 </summary>
public class CronDescribeInput
{
    /// <summary> Cron 表达式 </summary>
    public string CronExpression { get; set; }

    /// <summary> 语言类型，默认中文 </summary>
    public string? Language { get; set; } = "zh-CN";
}