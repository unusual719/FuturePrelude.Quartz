namespace FuturePrelude.Quartz;

/// <summary> Job 管理：持久化业务表后注册到 Quartz </summary>
public class JobManagementService : IJobManagementService
{
    private readonly IFreeSql _freeSql;
    private readonly ISchedulerService _schedulerService;
    private readonly ILogger<JobManagementService> _logger;

    public JobManagementService(IFreeSql freeSql, ISchedulerService schedulerService, ILogger<JobManagementService> logger)
    {
        _freeSql = freeSql;
        _schedulerService = schedulerService;
        _logger = logger;
    }

    /// <summary> JobKey 解析，支持 Group:Name 格式 </summary>
    private static (string Group, string Name) ParseJobKey(SysJobDetail job) => SchedulerEntityMapper.ParseJobKey(job);

    /// <summary> 根据 Quartz JobRuntimeState 转换为持久化的 JobStatus </summary>
    private static JobStatus ResolvePersistedJobStatus(JobRuntimeState jobState)
    {
        if (!jobState.Exists)
        {
            return JobStatus.NoSchedule;
        }

        if (jobState.IsRunning)
        {
            return JobStatus.Running;
        }

        if (jobState.IsPaused)
        {
            return JobStatus.Paused;
        }

        return jobState.TriggerState switch
        {
            TriggerState.None => JobStatus.NoTrigger,
            TriggerState.Complete => JobStatus.NoTrigger,
            TriggerState.Error => JobStatus.Error,
            _ => JobStatus.Idle
        };
    }

    private async Task<string> ResolveGroupKeyAsync(long groupId, CancellationToken cancellationToken)
    {
        if (groupId > 0)
        {
            var group = await _freeSql.Select<SysJobGroup>()
                .Where(x => x.Id == groupId && !x.IsDeleted)
                .FirstAsync(cancellationToken);

            if (group != null)
            {
                SchedulerIdentityHelper.NormalizeGroupIdentity(group);
                return group.Key;
            }
        }

        return SchedulerIdentityHelper.ResolveGroupKey(null, InternalConstants.DEFAULT_GROUP_NAME);
    }

    private async Task<string> SaveAndSchedulePluginJobInternalAsync(SchedulePluginJobRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.JobDetail);
        ArgumentNullException.ThrowIfNull(request.Plugin);
        ArgumentNullException.ThrowIfNull(request.Trigger);

        var job = request.JobDetail;
        var plugin = request.Plugin;
        var trigger = request.Trigger;

        var groupKey = await ResolveGroupKeyAsync(job.GroupId, cancellationToken);
        if (SchedulerIdentityHelper.TryParseCompositeKey(job.JobKey, out var existingJobGroup, out _)
            && SchedulerIdentityHelper.IsAsciiKey(existingJobGroup))
        {
            groupKey = existingJobGroup;
        }

        SchedulerIdentityHelper.NormalizeJobIdentity(job, groupKey);
        var (jobGroup, _) = ParseJobKey(job);

        trigger.TriggerGroup = string.IsNullOrWhiteSpace(trigger.TriggerGroup)
            ? jobGroup
            : trigger.TriggerGroup.Trim();

        var cronConfig = trigger.TriggerType == TriggerType.Cron
            ? TriggerConfigJsonHelper.DeserializeCron(trigger.TypeConfigJson)
            : null;

        TriggerConfigJsonHelper.NormalizeForSave(
            trigger,
            cronConfig?.CronExpression,
            cronConfig?.CronDescription,
            trigger.TimeZoneId);

        // 持久化：先业务库再注册 Quartz
        using var uow = _freeSql.CreateUnitOfWork();
        try
        {
            var freeSqlOrm = uow.Orm;

            // JobDetail
            if (job.Id == 0)
            {
                job.CreateTime = ChinaTimeZoneConverter.Now();
                job.UpdateTime = job.CreateTime;
                job.Id = (long)await freeSqlOrm.Insert(job)
                    .ExecuteIdentityAsync(cancellationToken);
            }
            else
            {
                job.UpdateTime = ChinaTimeZoneConverter.Now();
                await freeSqlOrm.Update<SysJobDetail>().SetSource(job)
                    .ExecuteAffrowsAsync(cancellationToken);
            }

            // Plugin
            plugin.JobId = job.Id;
            if (plugin.Id == 0)
            {
                plugin.CreateTime = ChinaTimeZoneConverter.Now();
                plugin.UpdateTime = plugin.CreateTime;
                plugin.Id = (long)await freeSqlOrm.Insert(plugin)
                    .ExecuteIdentityAsync(cancellationToken);
            }
            else
            {
                plugin.UpdateTime = ChinaTimeZoneConverter.Now();
                await freeSqlOrm.Update<SysJobPlugin>().SetSource(plugin)
                    .ExecuteAffrowsAsync(cancellationToken);
            }

            // Trigger
            trigger.JobId = job.Id;
            if (trigger.Id == 0)
            {
                trigger.CreateTime = ChinaTimeZoneConverter.Now();
                trigger.Id = (long)await freeSqlOrm.Insert(trigger)
                    .ExecuteIdentityAsync(cancellationToken);
            }
            else
            {
                trigger.UpdateTime = ChinaTimeZoneConverter.Now();
                await freeSqlOrm.Update<SysJobTrigger>().SetSource(trigger)
                    .ExecuteAffrowsAsync(cancellationToken);
            }

            uow.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存任务/插件/触发器失败");
            throw;
        }

        // 注册到 Quartz
        return await _schedulerService.SchedulePluginJobAsync(job, plugin, trigger, cancellationToken);
    }

    #region JobGroup - 分组相关操作

    /// <summary> 创建或更新分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<JobGroupOutput> SaveJobGroupAsync(JobGroupInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var group = input.Adapt<SysJobGroup>();
        await EnsureGroupNameUniqueAsync(group, cancellationToken);

        if (group.Id > 0)
        {
            var existingGroup = await _freeSql.Select<SysJobGroup>()
                .Where(x => x.Id == group.Id && !x.IsDeleted)
                .FirstAsync(cancellationToken);

            if (existingGroup == null)
            {
                throw new InvalidOperationException($"未匹配到当前分组信息:{group.Id}");
            }

            group.Key = existingGroup.Key;
        }

        SchedulerIdentityHelper.NormalizeGroupIdentity(group);

        if (group.Id == 0)
        {
            group.CreateTime = ChinaTimeZoneConverter.Now();
            group.Id = (long)await _freeSql.Insert(group).ExecuteIdentityAsync(cancellationToken);
        }
        else
        {
            group.UpdateTime = ChinaTimeZoneConverter.Now();
            await _freeSql.Update<SysJobGroup>().SetSource(group)
                .IgnoreColumns(c => new { c.CreateBy, c.CreateTime })
                .ExecuteAffrowsAsync(cancellationToken);
        }
        return group.Adapt<JobGroupOutput>();

        async Task EnsureGroupNameUniqueAsync(SysJobGroup group, CancellationToken cancellationToken)
        {
            var query = _freeSql.Select<SysJobGroup>()
                .Where(x => x.Name == group.Name && !x.IsDeleted);

            if (group.Id > 0)
            {
                query = query.Where(x => x.Id != group.Id);
            }

            var exists = await query.AnyAsync(cancellationToken);
            if (exists)
            {
                throw new InvalidOperationException($"分组名称 {group.Name} 已存在，无法保存。");
            }
        }
    }

    /// <summary> 删除分组 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> DeleteJobGroupAsync(JobGroupBaseInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var group = await _freeSql.Select<SysJobGroup>().Where(x => x.Id == input.Id).FirstAsync(cancellationToken);
        if (group == null) throw new InvalidOperationException($"未匹配到当前分组信息:{input.Id}");

        var any = await _freeSql.Select<SysJobDetail>()
             .Where(c => c.GroupId == group.Id).AnyAsync(cancellationToken);
        if (any) throw new InvalidOperationException($"分组 {group.Name} 下存在任务，不允许删除");

        group.UpdateTime = ChinaTimeZoneConverter.Now();
        group.IsDeleted = true;
        return (await _freeSql.Update<SysJobGroup>().SetSource(group)
            .UpdateColumns(c => new { c.IsDeleted, c.UpdateTime, c.UpdateBy })
            .ExecuteAffrowsAsync(cancellationToken)) > 0;
    }

    #endregion JobGroup - 分组相关操作

    #region JobDetail - 任务相关操作

    /// <summary> 插件类 Job 的创建/更新：先写业务表（Job/Plugin/Params/Trigger），再注册到 Quartz。 </summary>
    public async Task<PluginJobOutput> SaveAndSchedulePluginJobAsync(PluginJobInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Job);
        ArgumentNullException.ThrowIfNull(input.Plugin);
        ArgumentNullException.ThrowIfNull(input.Trigger);

        var request = input.Adapt<SchedulePluginJobRequest>();
        var jobKey = await SaveAndSchedulePluginJobInternalAsync(request, cancellationToken);

        return new PluginJobOutput
        {
            JobKey = jobKey,
            JobId = request.JobDetail.Id,
            TriggerId = request.Trigger.Id,
            PluginId = request.Plugin.Id
        };
    }

    /// <summary> 业务启用 Job + 恢复 Quartz </summary>
    public async Task EnableJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var job = await _freeSql.Select<SysJobDetail>().Where(x => x.Id == jobId).FirstAsync(cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} 不存在。");
        }

        var (group, name) = ParseJobKey(job);
        await _schedulerService.ResumeJobAsync(group, name, cancellationToken);

        var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);
        job.EnableStatus = EnableStatus.Enabled;
        job.JobStatus = ResolvePersistedJobStatus(jobState);
        job.UpdateTime = ChinaTimeZoneConverter.Now();
        await _freeSql.Update<SysJobDetail>()
            .SetSource(job)
            .ExecuteAffrowsAsync(cancellationToken);
    }

    /// <summary> 业务禁用 Job + 暂停 Quartz </summary>
    public async Task DisableJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var job = await _freeSql.Select<SysJobDetail>().Where(x => x.Id == jobId).FirstAsync(cancellationToken);
        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} 不存在。");
        }

        var (group, name) = ParseJobKey(job);
        await _schedulerService.PauseJobAsync(group, name, cancellationToken);

        var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);
        job.EnableStatus = EnableStatus.Disabled;
        job.JobStatus = ResolvePersistedJobStatus(jobState);
        job.UpdateTime = ChinaTimeZoneConverter.Now();
        await _freeSql.Update<SysJobDetail>().SetSource(job).ExecuteAffrowsAsync(cancellationToken);
    }

    /// <summary> 删除 Job（业务库清理 + Quartz 删除） </summary>
    public async Task DeleteJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var job = await _freeSql.Select<SysJobDetail>().Where(x => x.Id == jobId).FirstAsync(cancellationToken);
        if (job == null)
        {
            return;
        }

        var (group, name) = ParseJobKey(job);

        using var uow = _freeSql.CreateUnitOfWork();
        var orm = uow.Orm;
        try
        {
            await orm.Delete<SysJobTrigger>().Where(x => x.JobId == jobId).ExecuteAffrowsAsync(cancellationToken);
            await orm.Delete<SysJobPlugin>().Where(x => x.JobId == jobId).ExecuteAffrowsAsync(cancellationToken);
            await orm.Delete<SysJobDetail>().Where(x => x.Id == jobId).ExecuteAffrowsAsync(cancellationToken);
            uow.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除任务 {JobId} 失败", jobId);
            throw;
        }

        await _schedulerService.DeleteJobAsync(group, name, cancellationToken);
    }

    #endregion JobDetail - 任务相关操作
}
