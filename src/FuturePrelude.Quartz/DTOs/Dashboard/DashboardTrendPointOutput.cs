namespace FuturePrelude.Quartz;

/// <summary> 表示执行趋势图中的一个数据点 </summary>
public sealed class DashboardTrendPointOutput
{
    /// <summary> 时间桶时间点（如：每小时或每天的聚合时间） </summary>
    public DateTime BucketTime { get; set; }

    /// <summary> 成功执行次数 </summary>
    public int SuccessCount { get; set; }

    /// <summary> 失败执行次数 </summary>
    public int FailureCount { get; set; }
}