namespace FuturePrelude.Quartz;

/// <summary> 表示仪表盘告警信息 </summary>
public sealed class DashboardAlertOutput
{
    /// <summary> 告警标题 </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary> 告警详细描述 </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary> 告警严重程度（如：Info、Warn、Error、Critical） </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary> 告警创建时间 </summary>
    public DateTime CreatedAt { get; set; }
}