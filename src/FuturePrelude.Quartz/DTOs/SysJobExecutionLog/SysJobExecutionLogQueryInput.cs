namespace FuturePrelude.Quartz;

/// <summary> 任务执行记录列表查询入参 </summary>
public class SysJobExecutionLogQueryInput
{
    /// <summary> 任务ID </summary>
    public long? JobId { get; set; }

    /// <summary> 触发器ID </summary>
    public long? TriggerId { get; set; }

    /// <summary> 任务名称 </summary>
    public string? TaskName { get; set; }

    /// <summary> 执行结果/状态 </summary>
    public string? Result { get; set; }

    /// <summary> 开始时间 </summary>
    public DateTime? StartTime { get; set; }

    /// <summary> 结束时间 </summary>
    public DateTime? EndTime { get; set; }

    /// <summary> 页码 </summary>
    [DefaultValue(1)]
    public int PageIndex { get; set; } = 1;

    /// <summary> 每页条数 </summary>
    [DefaultValue(10)]
    public int PageSize { get; set; } = 10;
}