namespace FuturePrelude.Quartz.Controllers;

using CronExpressionDescriptor;

/// <summary> 任务触发器管理接口 </summary>
[ApiController]
[Route("api/sys-job-trigger")]
[Authorize]
public class SysJobTriggerController : Controller
{
    private readonly ISysJobTriggerService _sysJobTriggerService;

    public SysJobTriggerController(ISysJobTriggerService sysJobTriggerService)
    {
        _sysJobTriggerService = sysJobTriggerService;
    }

    /// <summary> 分页获取任务触发器列表 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("list")]
    [Description("获取任务触发器列表")]
    public async Task<PageResponse<TriggerOutput>> GetPageListAsync(
        [FromBody] SysJobTriggerQueryInput query,
        CancellationToken cancellationToken = default
    ) => await _sysJobTriggerService.GetPageListAsync(query, cancellationToken);

    /// <summary> 根据ID获取任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpGet("{id}")]
    [Description("根据ID获取任务触发器")]
    public async Task<TriggerOutput?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default
    ) => await _sysJobTriggerService.GetByIdAsync(id, cancellationToken);

    /// <summary> 创建任务触发器 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost]
    [Description("创建任务触发器")]
    public async Task<TriggerOutput> CreateAsync(
        SysJobTriggerCreateInput input,
        CancellationToken cancellationToken = default
    ) => await _sysJobTriggerService.CreateAsync(input, cancellationToken);

    /// <summary> 更新任务触发器 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPut]
    [Description("更新任务触发器")]
    public async Task<TriggerOutput> UpdateAsync(
        SysJobTriggerUpdateInput input,
        CancellationToken cancellationToken = default
    ) => await _sysJobTriggerService.UpdateAsync(input, cancellationToken);

    /// <summary> 删除任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpDelete("{id}")]
    [Description("删除任务触发器")]
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        await _sysJobTriggerService.DeleteAsync(id, cancellationToken);

    /// <summary> 启用任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/enable")]
    [Description("启用任务触发器")]
    public async Task<bool> EnableAsync(long id, CancellationToken cancellationToken = default) =>
        await _sysJobTriggerService.EnableAsync(id, cancellationToken);

    /// <summary> 禁用任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/disable")]
    [Description("禁用任务触发器")]
    public async Task<bool> DisableAsync(long id, CancellationToken cancellationToken = default) =>
        await _sysJobTriggerService.DisableAsync(id, cancellationToken);

    /// <summary> 解析 Cron 表达式为中文描述 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("describe-cron")]
    [Description("解析 Cron 表达式为中文描述")]
    public ApiResponse<string> DescribeCronExpression(
        [FromBody] CronDescribeInput input,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var description = CronExpressionDescriptor.ExpressionDescriptor.GetDescription(input.CronExpression
                , new Options()
                {
                    Locale = input.Language ?? "zh-CN"
                });
            return ApiResponse<string>.Ok($"{description}执行一次");
        }
        catch
        {
            return ApiResponse<string>.Ok(string.Empty);
        }
    }

    /// <summary> 立即触发任务执行（1分钟内只允许执行一次） </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("{id}/trigger-once")]
    [Description("立即触发任务执行（1分钟内只允许执行一次）")]
    public async Task<bool> TriggerOnceAsync(
        long id,
        CancellationToken cancellationToken = default
    ) => await _sysJobTriggerService.TriggerOnceAsync(id, cancellationToken);

    /// <summary> 批量获取触发器状态 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    [HttpPost("trigger-status-list")]
    [Description("批量获取触发器状态")]
    public async Task<List<TriggerStatusOutput>> GetTriggerStatusListAsync(
        [FromBody] TriggerStatusQueryInput input,
        CancellationToken cancellationToken = default
    ) => await _sysJobTriggerService.GetTriggerStatusListAsync(input.TriggerIds, cancellationToken);
}
