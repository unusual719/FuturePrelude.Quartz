namespace FuturePrelude.Quartz;

/// <summary> 仪表盘统计数据摘要 </summary>
public sealed class DashboardSummaryOutput
{
    /// <summary> 任务总数 </summary>
    public int TotalTasks { get; set; }

    /// <summary> 当前正在运行的任务数 </summary>
    public int RunningTasks { get; set; }

    /// <summary> 当前处于空闲状态的任务数 </summary>
    public int IdleTaskCount { get; set; }

    /// <summary> 今日失败任务数 </summary>
    public int FailedToday { get; set; }

    /// <summary> 平均响应时间（毫秒） </summary>
    public int AverageResponseMs { get; set; }

    /// <summary> 今日任务成功率（0-100） </summary>
    public decimal SuccessRate { get; set; }
}