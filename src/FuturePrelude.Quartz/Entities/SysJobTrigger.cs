namespace FuturePrelude.Quartz;

/// <summary> 任务触发器信息 </summary>
[Table(Name = "SysJobTrigger")]
[Index("idx_triggerkey", nameof(TriggerKey), true)]
[Index("idx_sysjobtrigger_jobid", nameof(JobId))]
[Index("idx_sysjobtrigger_enablestatus", nameof(EnableStatus))]
[Index("idx_sysjobtrigger_isdeleted", nameof(IsDeleted))]
[Index("idx_sysjobtrigger_time_range", nameof(StartTimeUtc) + "," + nameof(EndTimeUtc))]
[Index("idx_sysjobtrigger_prevfiretime", nameof(PrevFireTimeUtc))]
[Index("idx_sysjobtrigger_triggerstate", nameof(TriggerState))]
[Index("idx_sysjobtrigger_job_status_deleted", nameof(JobId) + "," + nameof(EnableStatus) + "," + nameof(IsDeleted))]
[Index("idx_sysjobtrigger_createtime", nameof(CreateTime) + " desc")]
public class SysJobTrigger
{
    /// <summary> Id 主键 </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary> 任务ID（SysJobDetail.Id） </summary>
    [Column(IsNullable = false)]
    public long JobId { get; set; }

    /// <summary> 触发器键（Group:Name） </summary>
    [Column(IsNullable = false)]
    public string TriggerKey { get; set; }

    /// <summary> 触发器名称 </summary>
    [Column(IsNullable = false)]
    public string TriggerName { get; set; }

    /// <summary> 触发器分组 </summary>
    [Column(IsNullable = false)]
    public string TriggerGroup { get; set; }

    /// <summary> 触发器类型（CRON, SIMPLE, CALENDAR, DAILY_TIME） </summary>
    [Column(IsNullable = false)]
    public TriggerType TriggerType { get; set; }

    /// <summary> 触发器有效期开始时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? StartTimeUtc { get; set; } = null;

    /// <summary> 触发器有效期结束时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? EndTimeUtc { get; set; } = null;

    /// <summary> 时区ID </summary>
    [Column(IsNullable = false)]
    public string TimeZoneId { get; set; } = TimeZoneDefaults.ChinaIanaTimeZone;

    /// <summary> 类型专属配置 JSON </summary>
    [Column(IsNullable = false)]
    public string TypeConfigJson { get; set; } = "{}";

    /// <summary> 错过触发时的处理策略（Misfire 策略） </summary>
    public MisfireAction MisfireStrategy { get; set; } = MisfireAction.SmartPolicy;

    /// <summary> 触发优先级 </summary>
    [Column(IsNullable = false)]
    public int Priority { get; set; } = 5;

    /// <summary> 上一次运行时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? PrevFireTimeUtc { get; set; }

    /// <summary> 下一次运行时间（UTC） </summary>
    [Column(IsNullable = true)]
    public DateTime? NextFireTimeUtc { get; set; }

    /// <summary> 触发次数 </summary>
    [Column(IsNullable = false)]
    public int FireCount { get; set; }

    /// <summary> 触发器运行状态 </summary>
    public TriggerState TriggerState { get; set; } = TriggerState.Normal;

    /// <summary> 启用状态（0-禁用, 1-启用） </summary>
    [Column(IsNullable = false)]
    public EnableStatus EnableStatus { get; set; } = EnableStatus.Enabled;

    /// <summary> 软删除标记 </summary>
    [Column(IsNullable = false)]
    public bool IsDeleted { get; set; } = false;

    /// <summary> 更新人 </summary>
    [Column(IsNullable = true)]
    public string UpdateBy { get; set; }

    /// <summary> 更新时间 </summary>
    [Column(IsNullable = true)]
    public DateTime? UpdateTime { get; set; }

    /// <summary> 任务创建人 </summary>
    [Column(IsNullable = true)]
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    [Column(IsNullable = false)]
    public DateTime CreateTime { get; set; } = ChinaTimeZoneConverter.Now();

    /// <summary> 触发器描述 </summary>
    [Column(IsNullable = true)]
    public string Description { get; set; }

    /// <summary> 任务详情信息 </summary>
    [Navigate(nameof(JobId))]
    public SysJobDetail JobDetail { get; set; }
}
