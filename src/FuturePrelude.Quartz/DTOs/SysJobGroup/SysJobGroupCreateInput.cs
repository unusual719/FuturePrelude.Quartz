namespace FuturePrelude.Quartz;

/// <summary> 创建任务分组入参 </summary>
public class SysJobGroupCreateInput
{
    /// <summary> 分组名称 </summary>
    public string Name { get; set; }

    /// <summary> 分组图标 </summary>
    public string Icon { get; set; }

    /// <summary> 状态（0-禁用, 1-启用） </summary>
    public EnableStatus? EnableStatus { get; set; }

    /// <summary> 分组描述 </summary>
    public string Description { get; set; }
}