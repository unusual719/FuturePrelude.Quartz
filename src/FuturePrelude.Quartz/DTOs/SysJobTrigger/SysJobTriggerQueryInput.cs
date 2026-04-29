namespace FuturePrelude.Quartz;

/// <summary> 任务触发器列表查询入参 </summary>
public class SysJobTriggerQueryInput
{
    /// <summary> 任务ID </summary>
    public long? JobId { get; set; }

    /// <summary> 启用状态 </summary>
    public EnableStatus? EnableStatus { get; set; }

    /// <summary> 关键词（搜索 TriggerName/TriggerGroup） </summary>
    public string? Keyword { get; set; }

    /// <summary> 页码 </summary>
    [DefaultValue(1)]
    public int PageIndex { get; set; } = 1;

    /// <summary> 每页条数 </summary>
    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;
}
