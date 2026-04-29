namespace FuturePrelude.Quartz;

/// <summary> 表示仪表盘健康状态分布数据 </summary>
public sealed class DashboardHealthDistributionOutput
{
    /// <summary> 状态标签（如：运行中、已完成、失败、等待中） </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary> 对应状态的任务数量 </summary>
    public int Count { get; set; }

    /// <summary> 图表中显示的颜色（如：#52c41a 表示绿色） </summary>
    public string Color { get; set; } = string.Empty;
}