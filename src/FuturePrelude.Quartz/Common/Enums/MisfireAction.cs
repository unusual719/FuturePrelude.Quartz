namespace FuturePrelude.Quartz;

using System.ComponentModel;

/// <summary> 错过触发处理策略 - 定义当触发器错过预定执行时间时的处理方式 </summary>
public enum MisfireAction
{
    /// <summary> 忽略错过策略 </summary>
    /// <remarks> 调度器永远不会评估错过情况，只会在可能时尽快执行，然后更新触发器如同按时执行一样 </remarks>
    [Description("忽略错过策略：调度器永远不会评估错过情况，只会在可能时尽快执行，然后更新触发器如同按时执行一样")]
    IgnoreMisfirePolicy,

    /// <summary> 指令未设置 - 尚未指定处理指令（默认状态） </summary>
    [Description("指令未设置：尚未指定处理指令")]
    InstructionNotSet,

    /// <summary> 智能策略 </summary>
    /// <remarks> 由Quartz根据触发器类型和配置自动选择最佳策略（推荐） </remarks>
    [Description("智能策略：由Quartz根据触发器类型和配置自动选择最佳策略")]
    SmartPolicy,

    /// <summary> 立即执行一次 </summary>
    /// <remarks> 立即执行一次。注意：通常仅用于单次（非重复）触发器 </remarks>
    [Description("立即执行一次：立即执行一次。注意：通常仅用于单次（非重复）触发器，用于重复触发器时等同于'立即调度（重算次数）'")]
    FireNow,

    /// <summary> 下次调度（保留次数） </summary>
    /// <remarks> 从当前时间后的下一个预定时间开始，保留剩余的重复次数不变 </remarks>
    [Description("下次调度（保留次数）：从当前时间后的下一个预定时间开始，保留剩余的重复次数不变")]
    RescheduleNextWithExistingCount,

    /// <summary> 下次调度（重算次数） </summary>
    /// <remarks> 从当前时间后的下一个预定时间开始，重新计算剩余的重复次数（就像从未错过任何执行一样） </remarks>
    [Description("下次调度（重算次数）：从当前时间后的下一个预定时间开始，重新计算剩余的重复次数，就像从未错过任何执行一样")]
    RescheduleNextWithRemainingCount,

    /// <summary> 立即调度（保留次数） </summary>
    /// <remarks> 现在立即执行，保留原有的重复次数。如果当前时间已超过结束时间则不会执行 </remarks>
    [Description("立即调度（保留次数）：现在立即执行，保留原有的重复次数。注意遵守触发器的结束时间，如果当前时间已超过结束时间则不会执行")]
    RescheduleNowWithExistingRepeatCount,

    /// <summary> 立即调度（重算次数） </summary>
    /// <remarks> 现在立即执行，重新计算剩余重复次数。注意：这会改变触发器原始的重复次数设置 </remarks>
    [Description("立即调度（重算次数）：现在立即执行，重新计算剩余重复次数。注意：这会改变触发器原始的重复次数设置，如果所有重复时间都已错过，执行后触发器可能变为'完成'状态")]
    RescheduleNowWithRemainingRepeatCount,

    /// <summary> 什么也不做 </summary>
    /// <remarks> 仅更新下次执行时间为当前时间后的下一个调度时间（考虑日历），但不立即执行 </remarks>
    [Description("什么也不做：仅更新下次执行时间为当前时间后的下一个调度时间（考虑日历），但不立即执行本次")]
    DoNothing,

    /// <summary> 立即执行一次 - 立即执行一次 </summary>
    [Description("立即执行一次：立即执行一次")]
    FireOnceNow
}
