namespace FuturePrelude.Quartz;

/// <summary> 创建任务详情入参 </summary>
public class SysJobDetailCreateInput
{
    /// <summary> 分组ID </summary>
    public long GroupId { get; set; }

    /// <summary> 任务名称 </summary>
    public string JobName { get; set; }

    /// <summary> 任务类型 </summary>
    public JobType JobType { get; set; }

    /// <summary> 任务描述 </summary>
    public string Description { get; set; }

    /// <summary> 禁止并发执行 </summary>
    public bool DisallowConcurrent { get; set; } = true;

    /// <summary> 失败是否重试 </summary>
    public bool RetryOnFailure { get; set; } = false;

    /// <summary> 最大重试次数 </summary>
    public int? MaxRetry { get; set; }

    /// <summary> 重试退避（秒） </summary>
    public int? RetryBackoffSeconds { get; set; }

    /// <summary> HTTP 配置（JobType=HttpApiJob 时必填） </summary>
    public HttpConfigInput? HttpConfig { get; set; }

    /// <summary> 插件配置（JobType=AssemblyPluginJob 时必填） </summary>
    public PluginConfigInput? PluginConfig { get; set; }
}