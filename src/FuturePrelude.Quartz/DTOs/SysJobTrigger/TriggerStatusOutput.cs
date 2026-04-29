namespace FuturePrelude.Quartz;

/// <summary> 触发器状态输出 </summary>
public class TriggerStatusOutput
{
    /// <summary> 触发器ID </summary>
    public long Id { get; set; }

    /// <summary> 触发器状态枚举 </summary>
    public TriggerState TriggerState { get; set; }

    /// <summary> 触发器状态 </summary>
    public string TriggerStateName { get; set; } = string.Empty;

    /// <summary> 触发器状态描述 </summary>
    public string Description { get; set; } = string.Empty;
}