using Microsoft.AspNetCore.SignalR;

namespace FuturePrelude.Quartz.Services;

/// <summary>
/// 仪表盘通知器实现
/// <para>通过 SignalR 向所有连接的客户端推送仪表盘数据变更通知。</para>
/// </summary>
public sealed class DashboardNotifier(
    IHubContext<DashboardHub> hubContext,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DashboardNotifier> logger) : IDashboardNotifier
{
    /// <summary> 节流延迟时间（毫秒），两次推送之间的最小间隔 </summary>
    private static readonly TimeSpan ThrottleDelay = TimeSpan.FromMilliseconds(300);

    private readonly IHubContext<DashboardHub> _hubContext = hubContext;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ILogger<DashboardNotifier> _logger = logger;

    /// <summary> 待处理通知标志（是否有新的变更需要推送） </summary>
    private int _pending;

    /// <summary> 是否正在推送循环中（防止重复启动后台任务） </summary>
    private int _draining;

    /// <inheritdoc />
    public Task NotifyOverviewChangedAsync(CancellationToken cancellationToken = default)
    {
        // 设置待处理标志，表示有新的变更需要推送
        Interlocked.Exchange(ref _pending, 1);

        // 如果已经在推送循环中，则直接返回 CompareExchange: 如果 draining==0，则设置为1，并返回0（表示成功获取了锁） 如果 draining!=0，则返回非0（表示已经有推送循环在运行）
        if (Interlocked.CompareExchange(ref _draining, 1, 0) != 0)
        {
            return Task.CompletedTask;
        }

        // 启动后台推送循环
        _ = Task.Run(async () =>
        {
            try
            {
                do
                {
                    // 等待节流延迟，避免频繁推送
                    await Task.Delay(ThrottleDelay, CancellationToken.None);

                    // 重置待处理标志
                    Interlocked.Exchange(ref _pending, 0);

                    // 通知推送前主动失效缓存，确保广播的是最新快照。
                    DashboardOverviewCache.Invalidate();

                    // 创建新的作用域，获取服务
                    using var scope = _serviceScopeFactory.CreateScope();
                    var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();

                    // 获取最新的仪表盘数据
                    var overview = await dashboardService.GetOverviewAsync(CancellationToken.None);

                    // 通过 SignalR 向所有连接的客户端广播数据
                    await _hubContext.Clients.All.SendAsync(
                        DashboardHub.OverviewUpdatedEvent,
                        overview,
                        CancellationToken.None);
                }
                // 如果在等待期间又有新的变更通知，循环继续
                while (Volatile.Read(ref _pending) == 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "广播 dashboard 概览快照失败");
            }
            finally
            {
                // 推送循环结束，重置 draining 标志
                Interlocked.Exchange(ref _draining, 0);

                // 如果在推送过程中又有新的变更通知（race condition），立即启动新的推送循环
                if (Volatile.Read(ref _pending) == 1)
                {
                    await NotifyOverviewChangedAsync(CancellationToken.None);
                }
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
