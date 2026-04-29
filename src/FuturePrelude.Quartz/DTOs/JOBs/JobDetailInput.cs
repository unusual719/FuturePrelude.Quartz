namespace FuturePrelude.Quartz;

/// <summary> Job 基础信息（入参） </summary>
public class JobDetailInput
{
    /// <summary> 任务分组ID </summary>
    public long GroupId { get; set; }

    /// <summary> Quartz JobKey（Group:Name 格式） </summary>
    public string JobKey { get; set; }

    /// <summary> 任务名称 </summary>
    public string JobName { get; set; }

    /// <summary> 任务类型（0=内置, 1=HTTP, 2=插件） </summary>
    public int JobType { get; set; }

    /// <summary> 任务状态（0=暂停, 1=正常） </summary>
    public int JobState { get; set; }

    /// <summary> 禁止并发执行（默认 true） </summary>
    public bool DisallowConcurrent { get; set; } = true;

    /// <summary> 失败是否重试 </summary>
    public bool RetryOnFailure { get; set; }

    /// <summary> Misfire 策略（默认 DoNothing） </summary>
    public MisfireAction MisfireStrategy { get; set; } = MisfireAction.DoNothing;

    /// <summary> 最大重试次数（可选） </summary>
    public int? MaxRetry { get; set; }

    /// <summary> 重试退避秒数（可选） </summary>
    public int? RetryBackoffSeconds { get; set; }

    /// <summary> 任务描述（可选） </summary>
    public string Description { get; set; }
}