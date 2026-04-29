namespace FuturePrelude.Quartz;

/// <summary> Quartz 任务分组信息 </summary>
[Table(Name = "SysJobGroup")]
[Index("idx_key", nameof(Key), true)]
[Index("idx_sysjobgroup_enablestatus", nameof(EnableStatus))]
[Index("idx_sysjobgroup_isdeleted", nameof(IsDeleted))]
[Index("idx_sysjobgroup_status_deleted", nameof(EnableStatus) + "," + nameof(IsDeleted))]
[Index("idx_sysjobgroup_createtime", nameof(CreateTime) + " desc")]
public class SysJobGroup
{
    /// <summary> Id 主键 </summary>
    [Column(IsPrimary = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary> 分组Key </summary>
    [Column(IsNullable = false)]
    public string Key { get; set; }

    /// <summary> 分组名称 </summary>
    [Column(IsNullable = false)]
    public string Name { get; set; }

    /// <summary> 分组图标 </summary>
    [Column(IsNullable = false)]
    public string ICon { get; set; }

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

    /// <summary> 排序 </summary>
    [Column(IsNullable = false)]
    public int SortOrder { get; set; } = 200;

    /// <summary> 当前分组下的任务详情 </summary>
    [Navigate(nameof(Id))]
    public List<SysJobDetail> JobDetails { get; set; }
}
