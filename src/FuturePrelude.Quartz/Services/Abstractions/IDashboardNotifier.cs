namespace FuturePrelude.Quartz.Services;

/// <summary>
/// 仪表盘通知器接口
/// <para> 定义向客户端推送仪表盘数据变更通知的相关操作 </para>
/// </summary>
public interface IDashboardNotifier
{
    /// <summary> 通知仪表盘数据已变更 </summary>
    /// <remarks> 当后端仪表盘数据发生变化时，调用此方法通知所有连接的客户端刷新数据 </remarks>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task NotifyOverviewChangedAsync(CancellationToken cancellationToken = default);
}
