namespace FuturePrelude.Quartz;

/// <summary> 触发器状态查询输入 </summary>
public class TriggerStatusQueryInput
{
    /// <summary> 触发器ID列表 </summary>
    public List<long> TriggerIds { get; set; } = [];
}