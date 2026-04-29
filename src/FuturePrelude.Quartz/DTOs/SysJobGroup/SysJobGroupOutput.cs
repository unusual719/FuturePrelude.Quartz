namespace FuturePrelude.Quartz;

/// <summary> 任务分组出参 </summary>
public class SysJobGroupOutput
{
    /// <summary> 分组主键 </summary>
    public long Id { get; set; }

    /// <summary> 分组名称 </summary>
    public string Name { get; set; }

    /// <summary> 分组图标 </summary>
    public string Icon { get; set; }

    /// <summary> 状态（0-禁用, 1-启用） </summary>
    public int Status { get; set; }

    /// <summary> 分组描述 </summary>
    public string Description { get; set; }

    /// <summary> 更新人 </summary>
    public string UpdateBy { get; set; }

    /// <summary> 更新时间 </summary>
    public DateTime? UpdateTime { get; set; }

    /// <summary> 创建人 </summary>
    public string CreateBy { get; set; }

    /// <summary> 创建时间 </summary>
    public DateTime CreateTime { get; set; }
}
