namespace FuturePrelude.Quartz;

/// <summary> 时间间隔单位枚举 - 定义触发器的重复执行间隔单位 </summary>
public enum IntervalUnit
{
    /// <summary> 毫秒 </summary>
    [Description("毫秒")]
    Millisecond,

    /// <summary> 秒 </summary>
    [Description("秒")]
    Second,

    /// <summary> 分钟 </summary>
    [Description("分钟")]
    Minute,

    /// <summary> 小时 </summary>
    [Description("小时")]
    Hour,

    /// <summary> 天 </summary>
    [Description("天")]
    Day,

    /// <summary> 周 </summary>
    [Description("周")]
    Week,

    /// <summary> 月 </summary>
    [Description("月")]
    Month,

    /// <summary> 年 </summary>
    [Description("年")]
    Year
}
