namespace FuturePrelude.Quartz.Controllers;

/// <summary> 任务分组管理接口 </summary>
[ApiController]
[Route("api/sys-job-group")]
[Authorize]
public class SysJobGroupController : Controller
{
    private readonly ISysJobGroupService _sysJobGroupService;

    public SysJobGroupController(ISysJobGroupService sysJobGroupService)
    {
        _sysJobGroupService = sysJobGroupService;
    }

    /// <summary> 获取任务分组列表 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpGet("list")]
    [Description("获取任务分组列表")]
    public async Task<List<SysJobGroupOutput>> GetListAsync(CancellationToken cancellationToken = default)
        => await _sysJobGroupService.GetListAsync(cancellationToken);

    /// <summary> 根据ID获取任务分组 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpGet("{id}")]
    [Description("根据ID获取任务分组")]
    public async Task<SysJobGroupOutput> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobGroupService.GetByIdAsync(id, cancellationToken);

    /// <summary> 创建任务分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost]
    [Description("创建任务分组")]
    public async Task<SysJobGroupOutput> CreateAsync(SysJobGroupCreateInput input, CancellationToken cancellationToken = default)
        => await _sysJobGroupService.CreateAsync(input, cancellationToken);

    /// <summary> 更新任务分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPut]
    [Description("更新任务分组")]
    public async Task<SysJobGroupOutput> UpdateAsync(SysJobGroupUpdateInput input, CancellationToken cancellationToken = default)
        => await _sysJobGroupService.UpdateAsync(input, cancellationToken);

    /// <summary> 删除任务分组 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpDelete("{id}")]
    [Description("删除任务分组")]
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
        => await _sysJobGroupService.DeleteAsync(id, cancellationToken);
}