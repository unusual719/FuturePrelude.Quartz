using Microsoft.AspNetCore.SignalR;

namespace FuturePrelude.Quartz;

/// <summary> 监控大盘实时推送 Hub </summary>
[Authorize]
public sealed class DashboardHub : Hub
{
    public const string OverviewUpdatedEvent = "dashboard-overview-updated";
}
