namespace FuturePrelude.Quartz;

/// <summary> Quartz Scheduler 调度器相关接口 </summary>
[ApiController]
[Route("api/scheduler")]
public class SchedulerController : ControllerBase
{
    // 负责已注册 Job 的运维操作（暂停/恢复/触发/删除/元数据）
    private readonly ISchedulerService _schedulerService;
    private readonly IDashboardNotifier _dashboardNotifier;

    public SchedulerController(
        ISchedulerService schedulerService,
        IDashboardNotifier dashboardNotifier)
    {
        _schedulerService = schedulerService;
        _dashboardNotifier = dashboardNotifier;
    }

    /// <summary> 暂停所有任务调度 </summary>
    /// <returns> </returns>
    [HttpPost("pause-all")]
    public async Task<bool> PauseAllSchedules()
    {
        await _schedulerService.PauseAllSchedules();
        await _dashboardNotifier.NotifyOverviewChangedAsync();
        return true;
    }

    /// <summary> 恢复所有任务调度 </summary>
    /// <returns> </returns>
    [HttpPost("resume-all")]
    public async Task<bool> ResumeAllSchedules()
    {
        await _schedulerService.ResumeAllSchedules();
        await _dashboardNotifier.NotifyOverviewChangedAsync();
        return true;
    }

    /// <summary> 暂停指定 Job（仅操作 Quartz，不改业务表） </summary>
    [HttpPost("{group}/{name}/pause")]
    public async Task<IActionResult> Pause(string group, string name, CancellationToken cancellationToken)
    {
        await _schedulerService.PauseJobAsync(group, name, cancellationToken);
        return NoContent();
    }

    /// <summary> 恢复指定 Job </summary>
    [HttpPost("{group}/{name}/resume")]
    public async Task<IActionResult> Resume(string group, string name, CancellationToken cancellationToken)
    {
        await _schedulerService.ResumeJobAsync(group, name, cancellationToken);
        return NoContent();
    }

    /// <summary> 立即触发指定 Job </summary>
    [HttpPost("{group}/{name}/trigger")]
    public async Task<IActionResult> TriggerNow(string group, string name, CancellationToken cancellationToken)
    {
        await _schedulerService.TriggerJobAsync(group, name, cancellationToken);
        return NoContent();
    }

    /// <summary> 删除指定 Job </summary>
    [HttpDelete("{group}/{name}")]
    public async Task<IActionResult> Delete(string group, string name, CancellationToken cancellationToken)
    {
        await _schedulerService.DeleteJobAsync(group, name, cancellationToken);
        return NoContent();
    }

    /// <summary> 获取 Quartz 调度器元数据 </summary>
    [HttpGet("metadata")]
    public async Task<ActionResult<SchedulerMetaData>> Metadata()
    {
        var data = await _schedulerService.GetMetadataAsync();
        return Ok(data);
    }
}
