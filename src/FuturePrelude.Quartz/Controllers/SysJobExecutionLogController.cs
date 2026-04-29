namespace FuturePrelude.Quartz.Controllers;

/// <summary> 任务执行记录管理接口 </summary>
[ApiController]
[Route("api/sys-job-execution-log")]
[Authorize]
public class SysJobExecutionLogController : Controller
{
    private readonly ISysJobExecutionLogService _sysJobExecutionLogService;

    public SysJobExecutionLogController(ISysJobExecutionLogService sysJobExecutionLogService)
    {
        _sysJobExecutionLogService = sysJobExecutionLogService;
    }

    /// <summary> 分页获取任务执行记录 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("list")]
    [Description("分页获取任务执行记录")]
    public async Task<PageResponse<SysJobExecutionLogOutput>> GetPageListAsync(
        [FromBody] SysJobExecutionLogQueryInput query,
        CancellationToken cancellationToken = default)
        => await _sysJobExecutionLogService.GetPageListAsync(query, cancellationToken);

    /// <summary> 删除近30天前的日志数据 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("clean-up")]
    [AllowAnonymous]
    [Description("删除近30天前的日志数据")]
    public async Task<int> DeleteLogsOlderThan30DaysAsync(CancellationToken cancellationToken = default)
        => await _sysJobExecutionLogService.DeleteLogsOlderThan30DaysAsync(cancellationToken);
}
