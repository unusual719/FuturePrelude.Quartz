namespace FuturePrelude.Quartz;

/// <summary> 业务层 Job 管理：先入库，再注册到 Quartz </summary>
public interface IJobManagementService
{
    #region JobGroup - 任务分组

    /// <summary> 新增/更新分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<JobGroupOutput> SaveJobGroupAsync(JobGroupInput input, CancellationToken cancellationToken = default);

    /// <summary> 删除分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<bool> DeleteJobGroupAsync(JobGroupBaseInput input, CancellationToken cancellationToken = default);

    #endregion JobGroup - 任务分组

    #region JobDetail - 任务详情

    /// <summary> 启用任务 </summary>
    /// <param name="jobId"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task EnableJobAsync(long jobId, CancellationToken cancellationToken = default);

    /// <summary> 禁用任务 </summary>
    /// <param name="jobId"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task DisableJobAsync(long jobId, CancellationToken cancellationToken = default);

    /// <summary> 删除任务 </summary>
    /// <param name="jobId"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task DeleteJobAsync(long jobId, CancellationToken cancellationToken = default);

    #endregion JobDetail - 任务详情

    Task<PluginJobOutput> SaveAndSchedulePluginJobAsync(PluginJobInput input, CancellationToken cancellationToken = default);
}