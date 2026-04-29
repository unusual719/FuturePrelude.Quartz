namespace FuturePrelude.Quartz;

/// <summary> 仪表盘概览完整输出模型 </summary>
public sealed class DashboardOverviewOutput
{
    /// <summary> 数据最后更新时间 </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary> 任务统计摘要 </summary>
    public DashboardSummaryOutput Summary { get; set; } = new();

    /// <summary> 执行趋势数据点列表（用于趋势图） </summary>
    public List<DashboardTrendPointOutput> Trend { get; set; } = [];

    /// <summary> 健康状态分布列表（用于饼图/柱状图） </summary>
    public List<DashboardHealthDistributionOutput> HealthDistribution { get; set; } = [];

    /// <summary> 最新告警列表 </summary>
    public List<DashboardAlertOutput> Alerts { get; set; } = [];

    /// <summary> 最近执行记录列表 </summary>
    public List<DashboardRecentExecutionOutput> RecentExecutions { get; set; } = [];
}