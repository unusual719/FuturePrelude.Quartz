namespace FuturePrelude.Quartz;

/// <summary> 任务详情信息 </summary>
[Table(Name = "SysJobDetail")]
[Index("idx_jobkey", nameof(JobKey), true)]
[Index("idx_sysjobdetail_groupid", nameof(GroupId))]
[Index("idx_sysjobdetail_enablestatus", nameof(EnableStatus))]
[Index("idx_sysjobdetail_isdeleted", nameof(IsDeleted))]
[Index("idx_sysjobdetail_group_status_deleted", nameof(GroupId) + "," + nameof(EnableStatus) + "," + nameof(IsDeleted))]
[Index("idx_sysjobdetail_createtime", nameof(CreateTime) + " desc")]
[Index("idx_sysjobdetail_lastrunstatus", nameof(LastRunStatus))]
[Index("idx_sysjobdetail_jobstatus", nameof(JobStatus))]
public class SysJobDetail
{
    /// <summary> Id 主键 </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary> 任务分组ID（SysJobGroup.Id） </summary>
    [Column(IsNullable = false)]
    public long GroupId { get; set; }

    /// <summary> Quartz JobKey（Group:Name） </summary>
    [Column(IsNullable = false)]
    public string JobKey { get; set; }

    /// <summary> 任务名称 </summary>
    [Column(IsNullable = false)]
    public string JobName { get; set; }

    /// <summary> 任务类型（内置/HTTP请求/插件热重载） </summary>
    [Column(IsNullable = false)]
    public JobType JobType { get; set; }

    /// <summary> 调度状态（运行时状态） </summary>
    [Column(IsNullable = true)]
    public JobStatus? JobStatus { get; set; }

    /// <summary> 禁止并发执行 </summary>
    [Column(IsNullable = false)]
    public bool DisallowConcurrent { get; set; } = true;

    /// <summary> 失败是否重试 </summary>
    [Column(IsNullable = false)]
    public bool RetryOnFailure { get; set; }

    /// <summary> 最大重试次数 </summary>
    [Column(IsNullable = true)]
    public int? MaxRetry { get; set; }

    /// <summary> 重试退避（秒） </summary>
    [Column(IsNullable = true)]
    public int? RetryBackoffSeconds { get; set; }

    /// <summary> 上次运行状态 </summary>
    [Column(IsNullable = true)]
    public RunStatus? LastRunStatus { get; set; } = RunStatus.Pending;

    /// <summary> 上次运行时间 </summary>
    [Column(IsNullable = true)]
    public DateTime? LastRunTime { get; set; }

    /// <summary> 上次运行耗时（毫秒） </summary>
    [Column(IsNullable = true)]
    public int? LastRunDurationMs { get; set; }

    /// <summary> 下一次触发时间 </summary>
    [Column(IsNullable = true)]
    public DateTime? NextFireTime { get; set; }

    /// <summary> 启用状态（0-禁用, 1-启用） </summary>
    [Column(IsNullable = false)]
    public EnableStatus EnableStatus { get; set; } = EnableStatus.Enabled;

    /// <summary> 更新人 </summary>
    [Column(IsNullable = true)]
    public string UpdateBy { get; set; }

    /// <summary> 更新时间 </summary>
    [Column(IsNullable = true)]
    public DateTime? UpdateTime { get; set; }

    /// <summary> 软删除标记 </summary>
    [Column(IsNullable = false)]
    public bool IsDeleted { get; set; } = false;

    /// <summary> 任务创建人 </summary>
    [Column(IsNullable = true)]
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    [Column(IsNullable = false)]
    public DateTime CreateTime { get; set; } = ChinaTimeZoneConverter.Now();

    /// <summary> 任务描述 </summary>
    [Column(IsNullable = true)]
    public string Description { get; set; }

    /// <summary> 任务分组信息 </summary>
    [Navigate(nameof(GroupId))]
    public SysJobGroup JobGroup { get; set; }

    /// <summary> 任务触发器详情信息 </summary>
    [Navigate(nameof(Id))]
    public List<SysJobTrigger> JobTriggers { get; set; }

    /// <summary> HttpApiJob 配置信息 </summary>
    [Navigate(nameof(Id))]
    public SysJobHttpConfig JobHttpConfig { get; set; }

    /// <summary> 插件作业配置信息 </summary>
    [Navigate(nameof(Id))]
    public SysJobPlugin JobPluginConfig { get; set; }
}