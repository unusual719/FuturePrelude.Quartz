namespace FuturePrelude.Quartz;

/// <summary> 任务状态批量查询入参 </summary>
public class JobStatusQueryInput
{
    /// <summary> 触发器ID列表 </summary>
    public List<long> Ids { get; set; } = [];
}
