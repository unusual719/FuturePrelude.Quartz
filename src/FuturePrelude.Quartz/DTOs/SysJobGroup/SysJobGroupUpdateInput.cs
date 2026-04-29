namespace FuturePrelude.Quartz;

/// <summary> 更新任务分组入参 </summary>
public class SysJobGroupUpdateInput
{
    /// <summary> 分组主键 </summary>
    public long Id { get; set; }

    /// <summary> 分组名称 </summary>
    public string Name { get; set; }

    /// <summary> 分组图标 </summary>
    public string Icon { get; set; }

    /// <summary> 状态（0-禁用, 1-启用） </summary>
    public EnableStatus? EnableStatus { get; set; }

    /// <summary> 分组描述 </summary>
    public string Description { get; set; }
}