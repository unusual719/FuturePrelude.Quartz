namespace FuturePrelude.Quartz.Services;

/// <summary>
/// 仪表盘服务接口
/// <para> 定义仪表盘数据获取的相关操作 </para>
/// </summary>
public interface IDashboardService
{
    /// <summary> 获取仪表盘概览数据 </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item> 统计摘要（任务总数、运行中任务、失败数、平均响应时间、成功率） </item>
    /// <item> 执行趋势数据（过去24小时每小时的成功/失败/运行中统计） </item>
    /// <item> 健康状态分布（运行中、已停止、空闲/异常任务的数量和颜色） </item>
    /// <item> 最近执行记录（最近8条任务执行日志） </item>
    /// <item> 告警列表（暂未实现） </item>
    /// </list>
    /// </remarks>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<DashboardOverviewOutput> GetOverviewAsync(CancellationToken cancellationToken = default);
}