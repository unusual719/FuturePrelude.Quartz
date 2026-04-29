namespace FuturePrelude.Quartz;

/// <summary> Scheduler 调度服务 </summary>
public interface ISchedulerService
{
    /// <summary> 恢复指定触发器 </summary>
    /// <param name="triggerName"> 触发器名称 </param>
    /// <param name="triggerGroup"> 触发器分组（可选） </param>
    Task ResumeTrigger(string triggerName, string? triggerGroup);

    /// <summary> 暂停指定触发器 </summary>
    /// <param name="triggerName"> 触发器名称 </param>
    /// <param name="triggerGroup"> 触发器分组（可选） </param>
    /// <remarks> </remarks>
    Task PauseTrigger(string triggerName, string? triggerGroup);

    /// <summary> 立即触发指定任务执行一次 </summary>
    /// <param name="jobName"> 任务名称 </param>
    /// <param name="jobGroup"> 任务分组 </param>
    /// <remarks> </remarks>
    Task TriggerJob(string jobName, string jobGroup);

    /// <summary> 根据 JobKey 查询运行状态 </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<JobRuntimeState> GetJobStateAsync(string group, string name, CancellationToken cancellationToken = default);

    /// <summary> 注册并调度一个插件 Job（附带触发器） </summary>
    /// <param name="job"> </param>
    /// <param name="plugin"> </param>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<string> SchedulePluginJobAsync(SysJobDetail job, SysJobPlugin plugin, SysJobTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary> 注册Http任务 </summary>
    /// <param name="job"> </param>
    /// <param name="httpConfig"> </param>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<string> ScheduleHttpJobAsync(SysJobDetail job, SysJobHttpConfig httpConfig, SysJobTrigger trigger
        , CancellationToken cancellationToken = default);

    /// <summary> 为已存在的 Job 增加一个 Trigger </summary>
    /// <param name="job"> 任务信息 </param>
    /// <param name="trigger"> 触发器信息 </param>
    /// <param name="cancellationToken"> 取消令牌 </param>
    /// <returns> </returns>
    Task<string> ScheduleTriggerAsync(SysJobDetail job, SysJobTrigger trigger, CancellationToken cancellationToken = default);

    /// <summary> 暂停 Job </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task PauseJobAsync(string group, string name, CancellationToken cancellationToken = default);

    /// <summary> 恢复 Job </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task ResumeJobAsync(string group, string name, CancellationToken cancellationToken = default);

    /// <summary> 立即触发 Job </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task TriggerJobAsync(string group, string name, CancellationToken cancellationToken = default);

    /// <summary> 删除 Job </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task DeleteJobAsync(string group, string name, CancellationToken cancellationToken = default);

    /// <summary> 获取调度器元数据 </summary>
    /// <returns> </returns>
    Task<SchedulerMetaData> GetMetadataAsync();

    /// <summary> 获取已调度任务统计信息 </summary>
    /// <returns> </returns>
    Task<IList<KeyValuePair<string, int>>> GetScheduledJobSummary(string groupKey);

    /// <summary> 暂停所有任务调度 </summary>
    Task PauseAllSchedules();

    /// <summary> 恢复所有任务调度 </summary>
    Task ResumeAllSchedules();

    /// <summary> 关闭调度器 </summary>
    Task ShutdownScheduler();

    /// <summary> 启动调度器 </summary>
    Task StartScheduler();

    /// <summary> 判断触发器是否存在 </summary>
    /// <param name="triggerName"> </param>
    /// <param name="triggerGroup"> </param>
    /// <returns> </returns>
    Task<bool> ContainsTriggerKey(string triggerName, string triggerGroup);

    /// <summary> 判断任务是否存在 </summary>
    /// <param name="jobName"> </param>
    /// <param name="jobGroup"> </param>
    /// <returns> </returns>
    Task<bool> ContainsJobKey(string jobName, string jobGroup);

    /// <summary> 删除任务及其触发器 </summary>
    /// <param name="job"> </param>
    /// <param name="trigger"> </param>
    /// <returns> </returns>
    Task<bool> DeleteSchedule(SysJobDetail job, SysJobTrigger trigger);

    /// <summary> 删除单个触发器 </summary>
    /// <param name="triggerName"> 触发器名称 </param>
    /// <param name="triggerGroup"> 触发器分组 </param>
    /// <param name="cancellationToken"> 取消令牌 </param>
    /// <returns> </returns>
    Task<bool> DeleteTriggerAsync(string triggerName, string triggerGroup, CancellationToken cancellationToken = default);

    /// <summary> 获取触发器状态 </summary>
    /// <param name="triggerName"> 触发器名称 </param>
    /// <param name="triggerGroup"> 触发器分组 </param>
    /// <param name="cancellationToken"> 取消令牌 </param>
    /// <returns> 触发器状态 </returns>
    Task<TriggerState> GetTriggerStateAsync(string triggerName, string triggerGroup, CancellationToken cancellationToken = default);
}