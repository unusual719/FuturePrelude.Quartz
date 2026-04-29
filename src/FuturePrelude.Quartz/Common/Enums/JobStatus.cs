namespace FuturePrelude.Quartz;

/// <summary> 任务状态 </summary>
public enum JobStatus
{
    /// <summary> 运行中 </summary>
    [Description("运行中")]
    Running,

    /// <summary> 空闲中 </summary>
    [Description("空闲中")]
    Idle,

    /// <summary> 暂停中 </summary>
    [Description("暂停中")]
    Paused,

    /// <summary> 此任务未分配触发器。当任务是持久的且触发器已结束时，会发生这种情况 </summary>
    [Description("无触发器")]
    NoTrigger,

    /// <summary> 没有在调度器中安排。当作业不是持久的并且触发器已结束时，会发生这种情况 </summary>
    [Description("未安排")]
    NoSchedule,

    /// <summary> 错误 </summary>
    [Description("错误")]
    Error
}
