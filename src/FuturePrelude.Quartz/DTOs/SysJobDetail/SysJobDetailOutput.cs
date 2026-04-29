namespace FuturePrelude.Quartz;

/// <summary> 任务详情出参 </summary>
public class SysJobDetailOutput
{
    /// <summary> 主键ID </summary>
    public long Id { get; set; }

    /// <summary> 分组ID </summary>
    public long GroupId { get; set; }

    /// <summary> Quartz JobKey </summary>
    public string JobKey { get; set; }

    /// <summary> 任务名称 </summary>
    public string JobName { get; set; }

    /// <summary> 任务类型 </summary>
    public JobType JobType { get; set; }

    /// <summary> 调度状态 </summary>
    public JobStatus? JobStatus { get; set; }

    /// <summary> 禁止并发执行 </summary>
    public bool DisallowConcurrent { get; set; }

    /// <summary> 失败是否重试 </summary>
    public bool RetryOnFailure { get; set; }

    /// <summary> 最大重试次数 </summary>
    public int? MaxRetry { get; set; }

    /// <summary> 重试退避（秒） </summary>
    public int? RetryBackoffSeconds { get; set; }

    /// <summary> 上次运行状态 </summary>
    public RunStatus? LastRunStatus { get; set; }

    /// <summary> 上次运行时间 </summary>
    public DateTime? LastRunTime { get; set; }

    /// <summary> 上次运行耗时（毫秒） </summary>
    public int? LastRunDurationMs { get; set; }

    /// <summary> 下一次触发时间 </summary>
    public DateTime? NextFireTime { get; set; }

    /// <summary> 启用状态 </summary>
    public EnableStatus EnableStatus { get; set; }

    /// <summary> 任务描述 </summary>
    public string Description { get; set; }

    /// <summary> 创建人 </summary>
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    public DateTime CreateTime { get; set; }

    /// <summary> 更新人 </summary>
    public string UpdateBy { get; set; }

    /// <summary> 更新时间 </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary> 任务分组信息 </summary>
    public SysJobGroupOutput JobGroup { get; set; }

    /// <summary> HTTP 配置信息 </summary>
    public HttpConfigOutput HttpConfig { get; set; }

    /// <summary> 插件配置信息 </summary>
    public PluginConfigOutput PluginConfig { get; set; }

    /// <summary> 触发器列表 </summary>
    public List<TriggerOutput> Triggers { get; set; } = [];
}