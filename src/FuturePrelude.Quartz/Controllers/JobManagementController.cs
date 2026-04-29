namespace FuturePrelude.Quartz;

/// <summary> 负责 Job/Trigger/Plugin 的创建与入库，再交由 JobManagementService 调度注册到 Quartz </summary>
[ApiController]
[Route("api")]
public class JobManagementController : ControllerBase
{
    private readonly IJobManagementService _jobManagementService;

    public JobManagementController(IJobManagementService jobManagementService)
    {
        _jobManagementService = jobManagementService;
    }

    #region JobGroup - 任务分组

    /// <summary> 创建或更新分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("group/save")]
    public async Task<JobGroupOutput> SaveGroupAsync([FromBody] JobGroupInput input
        , CancellationToken cancellationToken)
        => await _jobManagementService.SaveJobGroupAsync(input, cancellationToken);

    /// <summary> 删除分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("group/delete")]
    public async Task<bool> DeleteJobGroupAsync(JobGroupBaseInput input, CancellationToken cancellationToken = default)
        => await _jobManagementService.DeleteJobGroupAsync(input, cancellationToken);

    #endregion JobGroup - 任务分组

    /// <summary> 注册并调度插件任务（Cron） </summary>
    [HttpPost("plugin")]
    public async Task<ActionResult<PluginJobOutput>> SchedulePlugin([FromBody] PluginJobInput input, CancellationToken cancellationToken)
    {
        if (input?.Job == null || input.Plugin == null || input.Trigger == null)
        {
            return BadRequest("缺少任务、插件或触发器配置。");
        }

        var output = await _jobManagementService.SaveAndSchedulePluginJobAsync(input, cancellationToken);
        return Ok(output);
    }

    /// <summary> 启用 Job（业务状态 + 恢复 Quartz） </summary>
    [HttpPost("{jobId:long}/enable")]
    public async Task<IActionResult> Enable(long jobId, CancellationToken cancellationToken)
    {
        await _jobManagementService.EnableJobAsync(jobId, cancellationToken);
        return NoContent();
    }

    /// <summary> 禁用 Job（业务状态 + 暂停 Quartz） </summary>
    [HttpPost("{jobId:long}/disable")]
    public async Task<IActionResult> Disable(long jobId, CancellationToken cancellationToken)
    {
        await _jobManagementService.DisableJobAsync(jobId, cancellationToken);
        return NoContent();
    }

    /// <summary> 删除 Job（业务库 + Quartz） </summary>
    [HttpDelete("{jobId:long}")]
    public async Task<IActionResult> Delete(long jobId, CancellationToken cancellationToken)
    {
        await _jobManagementService.DeleteJobAsync(jobId, cancellationToken);
        return NoContent();
    }
}

public class SchedulePluginJobRequest
{
    public SysJobDetail JobDetail { get; set; }
    public SysJobPlugin Plugin { get; set; }
    public SysJobTrigger Trigger { get; set; }
}