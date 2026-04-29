namespace FuturePrelude.Quartz.Controllers;

/// <summary> 任务详情管理接口 </summary>
[ApiController]
[Route("api/sys-job-detail")]
[Authorize]
public class SysJobDetailController : Controller
{
    private readonly ISysJobDetailService _sysJobDetailService;
    private readonly ISysJobTriggerService _sysJobTriggerService;

    public SysJobDetailController(ISysJobDetailService sysJobDetailService
        , ISysJobTriggerService sysJobTriggerService)
    {
        _sysJobDetailService = sysJobDetailService;
        _sysJobTriggerService = sysJobTriggerService;
    }

    /// <summary> 分页获取任务详情列表 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("list")]
    [Description("获取任务详情列表")]
    public async Task<PageResponse<SysJobDetailOutput>> GetPageListAsync(
        [FromBody] SysJobDetailQueryInput query, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.GetPageListAsync(query, cancellationToken);

    /// <summary> 根据ID获取任务详情 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpGet("{id}")]
    [Description("根据ID获取任务详情")]
    public async Task<SysJobDetailOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.GetByIdAsync(id, cancellationToken);

    /// <summary> 创建任务详情 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost]
    [Description("创建任务详情")]
    public async Task<SysJobDetailOutput> CreateAsync(SysJobDetailCreateInput input, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.CreateAsync(input, cancellationToken);

    /// <summary> 更新任务详情 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPut]
    [Description("更新任务详情")]
    public async Task<SysJobDetailOutput> UpdateAsync(SysJobDetailUpdateInput input, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.UpdateAsync(input, cancellationToken);

    /// <summary> 删除任务详情 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpDelete("{id}")]
    [Description("删除任务详情")]
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.DeleteAsync(id, cancellationToken);

    /// <summary> 启用任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/enable")]
    [Description("启用任务")]
    public async Task<bool> EnableAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.EnableAsync(id, cancellationToken);

    /// <summary> 禁用任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/disable")]
    [Description("禁用任务")]
    public async Task<bool> DisableAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.DisableAsync(id, cancellationToken);

    /// <summary> 暂停任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/pause")]
    [Description("暂停任务")]
    public async Task<bool> PauseJobAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.PauseJobAsync(id, cancellationToken);

    /// <summary> 恢复任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/resume")]
    [Description("恢复任务")]
    public async Task<bool> ResumeJobAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobDetailService.ResumeJobAsync(id, cancellationToken);

    /// <summary> 批量获取任务状态 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("job-status")]
    [Description("批量获取任务状态")]
    public async Task<List<JobStatusOutput>> GetJobStatusAsync(
        [FromBody] JobStatusQueryInput input, CancellationToken cancellationToken = default)
        => await _sysJobTriggerService.GetJobStatusAsync(input, cancellationToken);
}
