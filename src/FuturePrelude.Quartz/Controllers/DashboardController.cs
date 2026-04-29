namespace FuturePrelude.Quartz.Controllers;

/// <summary> 监控大盘接口 </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(IDashboardService dashboardService) : Controller
{
    private readonly IDashboardService _dashboardService = dashboardService;

    /// <summary> 获取监控大盘概览快照 </summary>
    [HttpGet("overview")]
    [Description("获取监控大盘概览")]
    public Task<DashboardOverviewOutput> GetOverviewAsync(CancellationToken cancellationToken = default)
        => _dashboardService.GetOverviewAsync(cancellationToken);
}
