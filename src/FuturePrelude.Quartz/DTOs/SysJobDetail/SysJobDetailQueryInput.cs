namespace FuturePrelude.Quartz;

/// <summary> 任务详情列表查询入参 </summary>
public class SysJobDetailQueryInput
{
    /// <summary> 分组ID </summary>
    public long? GroupId { get; set; }

    /// <summary> 任务类型 </summary>
    public JobType? JobType { get; set; }

    /// <summary> 状态 </summary>
    public EnableStatus? EnableStatus { get; set; }

    /// <summary> 关键词（搜索 JobName） </summary>
    public string? Keyword { get; set; }

    /// <summary> 页码 </summary>
    [DefaultValue(1)]
    public int PageIndex { get; set; } = 1;

    /// <summary> 每页条数 </summary>
    [DefaultValue(20)]
    public int PageSize { get; set; } = 20;
}
