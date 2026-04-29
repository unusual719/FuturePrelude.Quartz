namespace FuturePrelude.Quartz.Services;

/// <summary> 任务触发器服务接口 </summary>
public interface ISysJobTriggerService
{
    /// <summary> 分页获取任务触发器列表 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<PageResponse<TriggerOutput>> GetPageListAsync(
        SysJobTriggerQueryInput query,
        CancellationToken cancellationToken = default);

    /// <summary> 根据ID获取任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<TriggerOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 创建任务触发器 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<TriggerOutput> CreateAsync(
        SysJobTriggerCreateInput input,
        CancellationToken cancellationToken = default);

    /// <summary> 更新任务触发器 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<TriggerOutput> UpdateAsync(
        SysJobTriggerUpdateInput input,
        CancellationToken cancellationToken = default);

    /// <summary> 删除任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 启用任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> EnableAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 禁用任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> DisableAsync(long id, CancellationToken cancellationToken = default);

    /// <summary> 批量获取任务状态 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<List<JobStatusOutput>> GetJobStatusAsync(JobStatusQueryInput input, CancellationToken cancellationToken = default);

    /// <summary> 立即触发任务执行（1分钟内只允许执行一次） </summary>
    /// <param name="triggerId"> 触发器ID </param>
    /// <param name="cancellationToken"> 取消令牌 </param>
    /// <returns> 成功返回 true，1分钟内重复触发返回 false </returns>
    Task<bool> TriggerOnceAsync(long triggerId, CancellationToken cancellationToken = default);

    /// <summary> 批量获取触发器状态 </summary>
    /// <param name="triggerIds"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<List<TriggerStatusOutput>> GetTriggerStatusListAsync(List<long> triggerIds, CancellationToken cancellationToken = default);
}