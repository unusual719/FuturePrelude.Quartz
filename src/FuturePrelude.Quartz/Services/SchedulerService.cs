using Quartz.Impl.Matchers;

namespace FuturePrelude.Quartz;

/// <summary> Scheduler 调度服务 </summary>
public class SchedulerService : ISchedulerService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(ILogger<SchedulerService> logger,
        ISchedulerFactory schedulerFactory)
    {
        _logger = logger;
        _schedulerFactory = schedulerFactory;
    }

    private static TriggerState AggregateTriggerState(IReadOnlyCollection<TriggerState> triggerStates)
    {
        if (triggerStates.Count == 0)
        {
            return TriggerState.None;
        }

        if (triggerStates.Any(state => state == TriggerState.Error))
        {
            return TriggerState.Error;
        }

        if (triggerStates.Any(state => state is TriggerState.Normal or TriggerState.Blocked))
        {
            return TriggerState.Normal;
        }

        if (triggerStates.Any(state => state == TriggerState.Paused)
            && triggerStates.All(state => state is TriggerState.Paused or TriggerState.Complete))
        {
            return TriggerState.Paused;
        }

        if (triggerStates.All(state => state == TriggerState.Complete))
        {
            return TriggerState.Complete;
        }

        if (triggerStates.Any(state => state == TriggerState.Paused))
        {
            return TriggerState.Paused;
        }

        return triggerStates.First();
    }

    /// <summary> 查询运行状态 </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<JobRuntimeState> GetJobStateAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var key = new JobKey(name, group);

        var exists = await scheduler.CheckExists(key);
        var executing = await scheduler.GetCurrentlyExecutingJobs();
        var isRunning = executing.Any(x => x.JobDetail.Key.Equals(key));

        var triggers = await scheduler.GetTriggersOfJob(key);
        var triggerStates = new List<TriggerState>(triggers.Count);
        foreach (var trigger in triggers)
        {
            triggerStates.Add(await scheduler.GetTriggerState(trigger.Key));
        }

        var triggerState = AggregateTriggerState(triggerStates);
        var isPaused = triggerStates.Count > 0
            && triggerStates.Any(state => state == TriggerState.Paused)
            && triggerStates.All(state => state is TriggerState.Paused or TriggerState.Complete);

        return new JobRuntimeState
        {
            Exists = exists,
            IsRunning = isRunning,
            TriggerState = triggerState,
            IsPaused = isPaused
        };
    }

    /// <summary> 注册插件任务 </summary>
    /// <param name="job"> </param>
    /// <param name="plugin"> </param>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<string> SchedulePluginJobAsync(SysJobDetail job, SysJobPlugin plugin, SysJobTrigger trigger
        , CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobDetail = SchedulerEntityMapper.BuildPluginJobDetail(job, plugin);
        var quartzTrigger = SchedulerEntityMapper.BuildTrigger(trigger, job);

        await scheduler.ScheduleJob(jobDetail, quartzTrigger, cancellationToken);

        // 暂停任务
        if (job.JobStatus == JobStatus.Paused)
        {
            await scheduler.PauseJob(jobDetail.Key, cancellationToken);
        }

        _logger.LogInformation("Plugin 任务已注册: {JobKey} -> {TriggerKey}", jobDetail.Key, quartzTrigger.Key);
        return jobDetail.Key.ToString();
    }

    /// <summary> 注册Http任务 </summary>
    /// <param name="job"> </param>
    /// <param name="httpConfig"> </param>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<string> ScheduleHttpJobAsync(SysJobDetail job, SysJobHttpConfig httpConfig, SysJobTrigger trigger
        , CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var jobDetail = SchedulerEntityMapper.BuildHttpJobDetail(job, httpConfig);
        var quartzTrigger = SchedulerEntityMapper.BuildTrigger(trigger, job);

        await scheduler.ScheduleJob(jobDetail, quartzTrigger, cancellationToken);

        // 暂停任务
        if (job.JobStatus == JobStatus.Paused)
        {
            await scheduler.PauseJob(jobDetail.Key, cancellationToken);
        }

        _logger.LogInformation("Http 任务已注册: {JobKey} -> {TriggerKey}", jobDetail.Key, quartzTrigger.Key);
        return jobDetail.Key.ToString();
    }

    /// <summary> 为已存在的 Job 增加一个 Trigger </summary>
    /// <param name="job"> </param>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<string> ScheduleTriggerAsync(
        SysJobDetail job,
        SysJobTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        var quartzTrigger = SchedulerEntityMapper.BuildTrigger(trigger, job);

        await scheduler.ScheduleJob(quartzTrigger, cancellationToken);

        _logger.LogInformation("触发器已注册: {TriggerKey} -> {JobKey}", quartzTrigger.Key, job.JobKey);
        return quartzTrigger.Key.ToString();
    }

    /// <summary> 暂停任务 </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task PauseJobAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.PauseJob(new JobKey(name, group), cancellationToken);
    }

    /// <summary> 恢复任务 </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task ResumeJobAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.ResumeJob(new JobKey(name, group), cancellationToken);
    }

    /// <summary> 触发任务 </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task TriggerJobAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.TriggerJob(new JobKey(name, group), cancellationToken);
    }

    /// <summary> 删除任务 </summary>
    /// <param name="group"> </param>
    /// <param name="name"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task DeleteJobAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.DeleteJob(new JobKey(name, group), cancellationToken);
    }

    /// <summary> 获取调度元数据 </summary>
    /// <returns> </returns>
    public async Task<SchedulerMetaData> GetMetadataAsync()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        return await scheduler.GetMetaData();
    }

    /// <inheritdoc />
    public async Task StartScheduler()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.Start();
    }

    /// <inheritdoc />
    public async Task StandbyScheduler()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.Standby();
    }

    /// <inheritdoc />
    public async Task ResumeAllSchedules()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ResumeAll();
    }

    /// <inheritdoc />
    public async Task ShutdownScheduler()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.Shutdown();
    }

    /// <inheritdoc />
    public async Task<IList<KeyValuePair<string, int>>> GetScheduledJobSummary(string groupKey)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var executingCount = (await scheduler.GetCurrentlyExecutingJobs()).Count;
        var jobCount = (await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup())).Count;
        var triggerCount = (await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup())).Count;

        var sysJobCount = (await scheduler.GetJobKeys(
            GroupMatcher<JobKey>.GroupEquals(groupKey))).Count;
        var sysTriggerCount = (await scheduler.GetTriggerKeys(
            GroupMatcher<TriggerKey>.GroupEquals(groupKey))).Count;

        return new List<KeyValuePair<string, int>>
        {
               new KeyValuePair<string, int>("Jobs", jobCount),
               new KeyValuePair<string, int>("Triggers", triggerCount),
               new KeyValuePair<string, int>("Executing", executingCount),
               new KeyValuePair<string, int>("System Jobs", sysJobCount),
               new KeyValuePair<string, int>("System Triggers", sysTriggerCount)
        };
    }

    /// <inheritdoc />
    public async Task TriggerJob(string jobName, string jobGroup)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.TriggerJob(new JobKey(jobName, jobGroup));
    }

    /// <inheritdoc />
    public async Task PauseTrigger(string triggerName, string? triggerGroup)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.PauseTrigger(triggerGroup == null ?
            new TriggerKey(triggerName) :
            new TriggerKey(triggerName, triggerGroup));
    }

    /// <inheritdoc />
    public async Task ResumeTrigger(string triggerName, string? triggerGroup)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ResumeTrigger(triggerGroup == null ?
            new TriggerKey(triggerName) :
            new TriggerKey(triggerName, triggerGroup));
    }

    /// <inheritdoc />
    public async Task<bool> ContainsTriggerKey(string triggerName, string triggerGroup)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        return await scheduler.CheckExists(new TriggerKey(triggerName, triggerGroup));
    }

    /// <inheritdoc />
    public async Task<bool> ContainsJobKey(string jobName, string jobGroup)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        return await scheduler.CheckExists(new JobKey(jobName, jobGroup));
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSchedule(SysJobDetail job, SysJobTrigger trigger)
    {
        var scheduler = await _schedulerFactory.GetScheduler();

        if (job.JobName == null)
            return false;

        if (job.JobStatus == JobStatus.NoSchedule)
            return true;

        var (group, name) = SchedulerEntityMapper.ParseJobKey(job);
        var jobKey = new JobKey(name, group);

        if (job.JobStatus == JobStatus.Error &&
            trigger.TriggerName == null)
        {
            _logger.LogInformation("Job [{jobGroup}.{jobName}] has no trigger name. " +
                "Cannot UncheduleJob by trigger, will delete job directly.", jobKey.Group, jobKey.Name);
            return await scheduler.DeleteJob(jobKey);
        }

        if (job.JobStatus == JobStatus.NoTrigger)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            if (!triggers.Any())
                return await scheduler.DeleteJob(jobKey);
            else
            {
                _logger.LogWarning("Cannot delete Job [{jobGroup}.{jobName}]. There are still {triggerCount}" +
                    " trigger(s) assigned to this job.", jobKey.Group, jobKey.Name,
                    triggers.Count);
                return false;
            }
        }

        if (trigger.TriggerName == null)
            return false;

        var (triggerGroup, triggerName) = SchedulerEntityMapper.ParseTriggerKey(trigger);
        var success = await scheduler.UnscheduleJob(new TriggerKey(triggerName, triggerGroup));

        if (success)
        {
            var triggers = await scheduler.GetTriggersOfJob(jobKey);
            if (!triggers.Any())
            {
                _logger.LogInformation("UnscheduleJob [{jobGroup}.{jobName}] has no more triggers. " +
                    "Determine if job was deleted.", jobKey.Group, jobKey.Name);

                if (await scheduler.CheckExists(jobKey))
                {
                    _logger.LogInformation("Manually delete job [{jobGroup}.{jobName}].", jobKey.Group, jobKey.Name);
                    return await scheduler.DeleteJob(jobKey);
                }
            }
        }

        return success;
    }

    /// <summary> 删除单个触发器 </summary>
    /// <param name="triggerName"> </param>
    /// <param name="triggerGroup"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> DeleteTriggerAsync(
        string triggerName,
        string triggerGroup,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        return await scheduler.UnscheduleJob(new TriggerKey(triggerName, triggerGroup), cancellationToken);
    }

    /// <inheritdoc />
    public async Task PauseAllSchedules()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        if (await IsAllPaused(scheduler))
            throw new Exception($"暂无可暂停的任务，当前所有任务都已处于暂停状态");

        await scheduler.PauseAll();
    }

    public async Task<bool> IsAllPaused(IScheduler scheduler)
    {
        var groupNames = await scheduler.GetTriggerGroupNames();

        foreach (var group in groupNames)
        {
            var keys = await scheduler.GetTriggerKeys(
                GroupMatcher<TriggerKey>.GroupEquals(group));

            foreach (var key in keys)
            {
                var state = await scheduler.GetTriggerState(key);

                if (state != TriggerState.Paused)
                    return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<TriggerState> GetTriggerStateAsync(
        string triggerName,
        string triggerGroup,
        CancellationToken cancellationToken = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
        return await scheduler.GetTriggerState(new TriggerKey(triggerName, triggerGroup), cancellationToken);
    }
}