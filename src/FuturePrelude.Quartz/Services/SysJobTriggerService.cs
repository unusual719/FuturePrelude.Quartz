using StackExchange.Redis;

namespace FuturePrelude.Quartz.Services;

/// <summary> 任务触发器服务实现 </summary>
public class SysJobTriggerService(
    IFreeSql freeSql,
    ISchedulerService schedulerService,
    IDatabase redis,
    ILogger<SysJobTriggerService> logger,
    IDashboardNotifier dashboardNotifier) : ISysJobTriggerService
{
    private readonly IFreeSql _freeSql = freeSql;
    private readonly ISchedulerService _schedulerService = schedulerService;
    private readonly IDatabase _redis = redis;
    private readonly ILogger<SysJobTriggerService> _logger = logger;
    private readonly IDashboardNotifier _dashboardNotifier = dashboardNotifier;

    #region private methods

    /// <summary> 构造触发器出参 </summary>
    /// <param name="entity"> </param>
    /// <returns> </returns>
    private static TriggerOutput BuildOutput(SysJobTrigger entity)
    {
        var output = new TriggerOutput
        {
            Id = entity.Id,
            JobId = entity.JobId,
            TriggerKey = entity.TriggerKey,
            TriggerName = entity.TriggerName,
            TriggerGroup = entity.TriggerGroup,
            TriggerType = entity.TriggerType,
            TimeZoneId = entity.TimeZoneId,
            MisfireStrategy = entity.MisfireStrategy,
            Priority = entity.Priority,
            StartTimeUtc = entity.StartTimeUtc,
            EndTimeUtc = entity.EndTimeUtc,
            PrevFireTimeUtc = entity.PrevFireTimeUtc,
            NextFireTimeUtc = entity.NextFireTimeUtc,
            EnableStatus = entity.EnableStatus,
            Description = entity.Description
        };

        if (entity.TriggerType == TriggerType.Cron)
        {
            var cronConfig = TriggerConfigJsonHelper.DeserializeCron(entity.TypeConfigJson);
            output.CronExpression = cronConfig.CronExpression;
            output.CronDescription = cronConfig.CronDescription;
            output.NextFireTimeUtc = GetNextRunningDateWithCronExpression(output.CronExpression, entity.TriggerType, entity.TimeZoneId);
        }

        return output;
    }

    /// <summary> 获取触发器默认分组 </summary>
    /// <param name="triggerGroup"> </param>
    /// <param name="job"> </param>
    /// <returns> </returns>
    private static string ResolveTriggerGroup(string? triggerGroup, SysJobDetail job)
    {
        if (!string.IsNullOrWhiteSpace(triggerGroup))
        {
            return triggerGroup.Trim();
        }

        var (group, _) = SchedulerEntityMapper.ParseJobKey(job);
        return group;
    }

    /// <summary> 修正 TriggerName / TriggerGroup / TriggerKey </summary>
    /// <param name="trigger"> </param>
    /// <param name="job"> </param>
    /// <param name="triggerGroup"> </param>
    private static void NormalizeTriggerIdentity(
        SysJobTrigger trigger,
        SysJobDetail job,
        string? triggerGroup = null)
    {
        var resolvedTriggerGroup = ResolveTriggerGroup(triggerGroup ?? trigger.TriggerGroup, job);
        SchedulerIdentityHelper.NormalizeTriggerIdentity(trigger, resolvedTriggerGroup);
    }

    /// <summary> 枚举触发器身份候选项（兼容 TriggerKey 和历史遗留的 TriggerGroup/TriggerName 分离字段） </summary>
    /// <param name="trigger"> </param>
    /// <returns> </returns>
    private static IEnumerable<(string Group, string Name)> EnumerateTriggerIdentityCandidates(SysJobTrigger trigger)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (SchedulerIdentityHelper.TryParseCompositeKey(trigger.TriggerKey, out var currentGroup, out var currentName)
            && seen.Add($"{currentGroup}:{currentName}"))
        {
            yield return (currentGroup, currentName);
        }

        var legacyGroup = trigger.TriggerGroup?.Trim();
        var legacyName = trigger.TriggerName?.Trim();
        if (!string.IsNullOrWhiteSpace(legacyGroup)
            && !string.IsNullOrWhiteSpace(legacyName)
            && seen.Add($"{legacyGroup}:{legacyName}"))
        {
            yield return (legacyGroup, legacyName);
        }
    }

    private static DateTime? GetNextRunningDateWithCronExpression(
        string cronExpression,
        TriggerType triggerType,
        string? timeZoneId = null)
    {
        var exp = new CronExpression(cronExpression);
        exp.TimeZone = TimeZoneDefaults.ResolveTimeZone(timeZoneId);
        var next = exp.GetNextValidTimeAfter(DateTimeOffset.UtcNow);

        if (next == null)
        {
            throw new ArgumentException($"Cron 表达式「{cronExpression}」无法计算下次运行时间");
        }

        return ChinaTimeZoneConverter.FromOffset(next.Value);
    }

    /// <summary> 在 Quartz Scheduler 中查找触发器是否存在 </summary>
    /// <param name="trigger"> </param>
    /// <returns> </returns>
    private async Task<(bool Exists, string Group, string Name)> FindScheduledTriggerIdentityAsync(SysJobTrigger trigger)
    {
        foreach (var candidate in EnumerateTriggerIdentityCandidates(trigger))
        {
            if (await _schedulerService.ContainsTriggerKey(candidate.Name, candidate.Group))
            {
                return (true, candidate.Group, candidate.Name);
            }
        }

        return (false, string.Empty, string.Empty);
    }

    /// <summary> 从 Quartz Scheduler 中删除触发器 </summary>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    private async Task DeleteScheduledTriggersIfExistsAsync(SysJobTrigger trigger, CancellationToken cancellationToken)
    {
        foreach (var candidate in EnumerateTriggerIdentityCandidates(trigger))
        {
            if (await _schedulerService.ContainsTriggerKey(candidate.Name, candidate.Group))
            {
                await _schedulerService.DeleteTriggerAsync(candidate.Name, candidate.Group, cancellationToken);
            }
        }
    }

    /// <summary> 暂停 Quartz Scheduler 中的触发器 </summary>
    /// <param name="trigger"> </param>
    private async Task PauseScheduledTriggersIfExistsAsync(SysJobTrigger trigger)
    {
        foreach (var candidate in EnumerateTriggerIdentityCandidates(trigger))
        {
            if (await _schedulerService.ContainsTriggerKey(candidate.Name, candidate.Group))
            {
                await _schedulerService.PauseTrigger(candidate.Name, candidate.Group);
            }
        }
    }

    /// <summary> 获取任务实体，不存在则抛错 </summary>
    /// <param name="jobId"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    /// <exception cref="KeyNotFoundException"> </exception>
    private async Task<SysJobDetail> GetJobRequiredAsync(long jobId, CancellationToken cancellationToken)
    {
        var job = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == jobId && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        return job ?? throw new KeyNotFoundException($"任务 ID「{jobId}」不存在");
    }

    /// <summary> 获取触发器实体，不存在则抛错 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    /// <exception cref="KeyNotFoundException"> </exception>
    private async Task<SysJobTrigger> GetTriggerRequiredAsync(long id, CancellationToken cancellationToken)
    {
        var trigger = await _freeSql.Select<SysJobTrigger>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        return trigger ?? throw new KeyNotFoundException($"触发器 ID「{id}」不存在");
    }

    /// <summary> 校验 TriggerKey 唯一性 </summary>
    /// <param name="triggerKey"> </param>
    /// <param name="excludeId"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    /// <exception cref="ArgumentException"> </exception>
    private async Task EnsureTriggerKeyUniqueAsync(
        string triggerKey,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _freeSql.Select<SysJobTrigger>()
            .Where(a => a.TriggerKey == triggerKey && !a.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        if (await query.AnyAsync(cancellationToken))
        {
            throw new ArgumentException($"触发器键「{triggerKey}」已存在");
        }
    }

    /// <summary> 确保启用的触发器已注册到 Quartz Scheduler </summary>
    /// <param name="uowOrm"> </param>
    /// <param name="job"> </param>
    /// <param name="trigger"> </param>
    /// <param name="skipTriggerExistenceCheck"> </param>
    /// <param name="cancellationToken"> </param>
    private async Task EnsureEnabledTriggerScheduledAsync(
        IFreeSql uowOrm,
        SysJobDetail job,
        SysJobTrigger trigger,
        bool skipTriggerExistenceCheck,
        CancellationToken cancellationToken)
    {
        /*
         * 业务逻辑：
         * 1. 如果 skipTriggerExistenceCheck=false，先检查触发器是否已调度，是则直接返回
         * 2. 查询同一 Job 下所有启用的触发器
         * 3. 如果 Job 未注册到 Scheduler，先注册 Job 及主触发器，再批量注册其他触发器
         * 4. 如果 Job 已存在，直接调度当前触发器
         * **/

        if (!skipTriggerExistenceCheck)
        {
            var scheduledIdentity = await FindScheduledTriggerIdentityAsync(trigger);
            if (scheduledIdentity.Exists)
            {
                return;
            }
        }

        var activeTriggers = await uowOrm.Select<SysJobTrigger>()
            .Where(a => a.JobId == job.Id && !a.IsDeleted && a.EnableStatus == EnableStatus.Enabled)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        if (activeTriggers.Count == 0)
        {
            return;
        }

        var primaryTrigger = activeTriggers.FirstOrDefault(a => a.Id == trigger.Id) ?? activeTriggers[0];
        var (jobGroup, jobName) = SchedulerEntityMapper.ParseJobKey(job);

        if (!await _schedulerService.ContainsJobKey(jobName, jobGroup))
        {
            await ScheduleJobWithTriggerAsync(job, primaryTrigger, cancellationToken);

            foreach (var activeTrigger in activeTriggers.Where(a => a.Id != primaryTrigger.Id))
            {
                if (!(await FindScheduledTriggerIdentityAsync(activeTrigger)).Exists)
                {
                    await _schedulerService.ScheduleTriggerAsync(job, activeTrigger, cancellationToken);
                }
            }

            return;
        }

        await _schedulerService.ScheduleTriggerAsync(job, trigger, cancellationToken);
    }

    /// <summary> 按任务类型补注册 Quartz Job 与触发器 </summary>
    /// <param name="job"> </param>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <exception cref="InvalidOperationException"> </exception>
    /// <exception cref="NotSupportedException"> </exception>
    private async Task ScheduleJobWithTriggerAsync(
        SysJobDetail job,
        SysJobTrigger trigger,
        CancellationToken cancellationToken)
    {
        /*
         * 业务逻辑：
         * 1. HttpApiJob 类型：查询 HttpConfig，调用 schedulerService.ScheduleHttpJobAsync
         * 2. AssemblyPluginJob 类型：查询 PluginConfig，调用 schedulerService.SchedulePluginJobAsync
         * 3. 其他类型抛出 NotSupportedException
         * 4. 如果缺少必要配置抛出 InvalidOperationException
         * **/

        if (job.JobType == JobType.HttpApiJob)
        {
            var httpConfig = await _freeSql.Select<SysJobHttpConfig>()
                .Where(a => a.JobId == job.Id && !a.IsDeleted)
                .OrderByDescending(a => a.Id)
                .FirstAsync(cancellationToken);

            if (httpConfig == null)
            {
                throw new InvalidOperationException($"任务 ID「{job.Id}」缺少 HttpConfig 配置");
            }

            await _schedulerService.ScheduleHttpJobAsync(job, httpConfig, trigger, cancellationToken);
            return;
        }

        if (job.JobType == JobType.AssemblyPluginJob)
        {
            var pluginConfig = await _freeSql.Select<SysJobPlugin>()
                .Where(a => a.JobId == job.Id)
                .OrderByDescending(a => a.Id)
                .FirstAsync(cancellationToken);

            if (pluginConfig == null)
            {
                throw new InvalidOperationException($"任务 ID「{job.Id}」缺少 PluginConfig 配置");
            }

            await _schedulerService.SchedulePluginJobAsync(job, pluginConfig, trigger, cancellationToken);
            return;
        }

        throw new NotSupportedException($"任务类型「{job.JobType}」暂不支持触发器调度");
    }

    /// <summary> 校验 Cron 表达式的有效性（仅在 TriggerType.Cron 时进行校验） </summary>
    /// <param name="cronExpression"> </param>
    /// <param name="triggerType"> </param>
    /// <returns> </returns>
    /// <exception cref="ArgumentException"> </exception>
    public bool ValidateCronExpression(string cronExpression, TriggerType triggerType)
    {
        if (triggerType == TriggerType.Cron)
        {
            if (!CronExpression.IsValidExpression(cronExpression))
            {
                throw new ArgumentException($"Cron 表达式「{cronExpression}」无效");
            }
        }

        return true;
    }

    #endregion private methods

    /// <summary> 转换触发器状态为业务状态 </summary>
    /// <param name="state"> </param>
    /// <returns> </returns>
    private static (string Status, string Description) ConvertTriggerState(TriggerState state)
    {
        return state switch
        {
            TriggerState.None => ("未注册", "触发器未注册到调度器"),
            TriggerState.Normal => ("正常", "触发器处于正常等待状态"),
            TriggerState.Paused => ("暂停", "触发器暂停触发"),
            TriggerState.Complete => ("已完成", "触发器已触发完毕，不再执行"),
            TriggerState.Error => ("错误", "触发器处于错误状态"),
            TriggerState.Blocked => ("阻塞", "触发器被阻塞，无法执行"),
            _ => ("未知", $"未知状态: {state}")
        };
    }

    /// <summary> 分页获取任务触发器列表 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<PageResponse<TriggerOutput>> GetPageListAsync(
        SysJobTriggerQueryInput query,
        CancellationToken cancellationToken = default)
    {
        var select = _freeSql.Select<SysJobTrigger>()
            .Where(a => !a.IsDeleted)
            .Where(a => a.JobId == query.JobId.Value);

        if (query.EnableStatus.HasValue)
            select = select.Where(a => a.EnableStatus == query.EnableStatus.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            select = select.Where(a => a.TriggerName.Contains(keyword) || a.TriggerGroup.Contains(keyword));
        }

        var totalCount = await select.CountAsync(cancellationToken);
        var entities = await select
            .OrderBy(a => a.Id)
            .Page(query.PageIndex, query.PageSize)
            .ToListAsync(cancellationToken);

        var pageResponse = new PageResponse<TriggerOutput>
        {
            Items = entities.Select(BuildOutput).ToList(),
            TotalCount = (int)totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };

        foreach (var item in pageResponse.Items)
        {
            var tigger = await GetTriggerStatusListAsync(new List<long> { item.Id });
            if (tigger.Count > 0)
            {
                item.TriggerState = tigger[0].TriggerState;
                item.TriggerStateName = tigger[0].TriggerStateName;
            }
        }

        return pageResponse;
    }

    /// <summary> 根据ID获取任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<TriggerOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _freeSql.Select<SysJobTrigger>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        return entity == null ? null : BuildOutput(entity);
    }

    /// <summary> 创建任务触发器 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<TriggerOutput> CreateAsync(
        SysJobTriggerCreateInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateCronExpression(input.CronExpression, input.TriggerType);

        var job = await GetJobRequiredAsync(input.JobId, cancellationToken);
        var entity = new SysJobTrigger
        {
            JobId = job.Id,
            TriggerName = input.TriggerName,
            TriggerGroup = ResolveTriggerGroup(input.TriggerGroup, job),
            TriggerType = input.TriggerType,
            StartTimeUtc = ChinaTimeZoneConverter.NormalizeToChinaTime(input.StartTimeUtc),
            EndTimeUtc = ChinaTimeZoneConverter.NormalizeToChinaTime(input.EndTimeUtc),
            MisfireStrategy = input.MisfireStrategy,
            Priority = input.Priority,
            EnableStatus = input.EnableStatus,
            Description = input.Description,
            CreateBy = "system",
            NextFireTimeUtc = input.TriggerType == TriggerType.Cron
                ? GetNextRunningDateWithCronExpression(input.CronExpression, input.TriggerType, input.TimeZoneId)
                : null,
            CreateTime = ChinaTimeZoneConverter.Now()
        };

        NormalizeTriggerIdentity(entity, job, input.TriggerGroup);

        TriggerConfigJsonHelper.NormalizeForSave(
            entity,
            input.CronExpression,
            input.CronDescription,
            input.TimeZoneId);

        await EnsureTriggerKeyUniqueAsync(entity.TriggerKey, null, cancellationToken);

        using var uow = _freeSql.CreateUnitOfWork();
        var orm = uow.Orm;

        entity.Id = (long)await orm.Insert(entity).ExecuteIdentityAsync(cancellationToken);

        if (entity.EnableStatus == EnableStatus.Enabled)
        {
            await EnsureEnabledTriggerScheduledAsync(orm, job, entity, false, cancellationToken);
        }

        uow.Commit();
        await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        return BuildOutput(entity);
    }

    /// <summary> 更新任务触发器 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<TriggerOutput> UpdateAsync(
        SysJobTriggerUpdateInput input,
        CancellationToken cancellationToken = default)
    {
        ValidateCronExpression(input.CronExpression, input.TriggerType);

        using var uow = _freeSql.CreateUnitOfWork();
        var orm = uow.Orm;

        var entity = await GetTriggerRequiredAsync(input.Id, cancellationToken);
        var job = await GetJobRequiredAsync(entity.JobId, cancellationToken);

        var previousIdentity = new SysJobTrigger
        {
            TriggerKey = entity.TriggerKey,
            TriggerName = entity.TriggerName,
            TriggerGroup = entity.TriggerGroup
        };
        await DeleteScheduledTriggersIfExistsAsync(previousIdentity, cancellationToken);

        entity.TriggerName = input.TriggerName;
        entity.TriggerGroup = ResolveTriggerGroup(input.TriggerGroup, job);
        entity.TriggerType = input.TriggerType;
        entity.StartTimeUtc = ChinaTimeZoneConverter.NormalizeToChinaTime(input.StartTimeUtc);
        entity.EndTimeUtc = ChinaTimeZoneConverter.NormalizeToChinaTime(input.EndTimeUtc);
        entity.MisfireStrategy = input.MisfireStrategy;
        entity.Priority = input.Priority;
        entity.Description = input.Description;
        entity.UpdateBy = "system";
        entity.UpdateTime = ChinaTimeZoneConverter.Now();
        // 下一次运行时间
        entity.NextFireTimeUtc = input.TriggerType == TriggerType.Cron
            ? GetNextRunningDateWithCronExpression(input.CronExpression, input.TriggerType, input.TimeZoneId)
            : null;

        NormalizeTriggerIdentity(entity, job, input.TriggerGroup);

        TriggerConfigJsonHelper.NormalizeForSave(
            entity,
            input.CronExpression,
            input.CronDescription,
            input.TimeZoneId);

        await EnsureTriggerKeyUniqueAsync(entity.TriggerKey, entity.Id, cancellationToken);

        await _freeSql.Update<SysJobTrigger>()
            .SetSource(entity)
            .ExecuteAffrowsAsync(cancellationToken);

        if (entity.EnableStatus == EnableStatus.Enabled)
        {
            await EnsureEnabledTriggerScheduledAsync(orm, job, entity, true, cancellationToken);
        }

        uow.Commit();
        await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        return BuildOutput(entity);
    }

    /// <summary> 删除任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTriggerRequiredAsync(id, cancellationToken);
        await DeleteScheduledTriggersIfExistsAsync(entity, cancellationToken);

        var rows = await _freeSql.Delete<SysJobTrigger>()
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 启用任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> EnableAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTriggerRequiredAsync(id, cancellationToken);
        var job = await GetJobRequiredAsync(entity.JobId, cancellationToken);

        NormalizeTriggerIdentity(entity, job);
        entity.EnableStatus = EnableStatus.Enabled;
        entity.UpdateBy = "system";
        entity.UpdateTime = ChinaTimeZoneConverter.Now();

        var rows = await _freeSql.Update<SysJobTrigger>()
            .Set(a => a.TriggerKey, entity.TriggerKey)
            .Set(a => a.TriggerName, entity.TriggerName)
            .Set(a => a.TriggerGroup, entity.TriggerGroup)
            .Set(a => a.EnableStatus, EnableStatus.Enabled)
            .Set(a => a.UpdateBy, entity.UpdateBy)
            .Set(a => a.UpdateTime, entity.UpdateTime)
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        var scheduledIdentity = await FindScheduledTriggerIdentityAsync(entity);
        if (scheduledIdentity.Exists)
        {
            await _schedulerService.ResumeTrigger(scheduledIdentity.Name, scheduledIdentity.Group);
        }
        else
        {
            await EnsureEnabledTriggerScheduledAsync(_freeSql, job, entity, false, cancellationToken);
        }

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 禁用任务触发器 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> DisableAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetTriggerRequiredAsync(id, cancellationToken);
        var job = await GetJobRequiredAsync(entity.JobId, cancellationToken);
        NormalizeTriggerIdentity(entity, job);
        await PauseScheduledTriggersIfExistsAsync(entity);

        var rows = await _freeSql.Update<SysJobTrigger>()
            .Set(a => a.TriggerKey, entity.TriggerKey)
            .Set(a => a.TriggerName, entity.TriggerName)
            .Set(a => a.TriggerGroup, entity.TriggerGroup)
            .Set(a => a.EnableStatus, EnableStatus.Disabled)
            .Set(a => a.UpdateBy, "system")
            .Set(a => a.UpdateTime, ChinaTimeZoneConverter.Now())
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 根据 JobId 查询 JobStatus 列表 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<List<JobStatusOutput>> GetJobStatusAsync(JobStatusQueryInput input,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _freeSql.Select<SysJobDetail>()
            .Where(a => !a.IsDeleted && input.Ids.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var jobMap = jobs.ToDictionary(j => j.Id);

        var results = new List<JobStatusOutput>();

        foreach (var job in jobs)
        {
            var (group, name) = SchedulerEntityMapper.ParseJobKey(job);
            var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);

            var status = jobState.IsRunning ? "运行中"
                : jobState.IsPaused ? "暂停中"
                : jobState.Exists ? "空闲中" : "未知";
            results.Add(new JobStatusOutput { Id = job.Id, Status = status });
        }

        return results;
    }

    /// <summary> 立即触发任务执行（1分钟内只允许执行一次） </summary>
    /// <param name="triggerId"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> TriggerOnceAsync(long triggerId, CancellationToken cancellationToken = default)
    {
        var trigger = await GetTriggerRequiredAsync(triggerId, cancellationToken);
        var job = await GetJobRequiredAsync(trigger.JobId, cancellationToken);

        // 构建 Redis 锁 key，限制 1 分钟内不能重复触发
        var lockKey = $"trigger:once:{triggerId}".BuildRedisKey();
        var lockValue = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

        // 尝试设置锁，60 秒过期
        var acquired = await _redis.StringSetAsync(
            lockKey,
            lockValue,
            TimeSpan.FromSeconds(60),
            When.NotExists,
            CommandFlags.None);

        if (!acquired)
        {
            _logger.LogWarning("触发器 ID「{TriggerId}」在1分钟内已触发过，拒绝重复触发", triggerId);
            return false;
        }

        try
        {
            // 获取 job 的 group 和 name
            var (group, name) = SchedulerEntityMapper.ParseJobKey(job);

            // 调用 Quartz 立即触发执行
            await _schedulerService.TriggerJobAsync(group, name, cancellationToken);

            _logger.LogInformation("触发器 ID「{TriggerId}」已成功触发任务执行", triggerId);
            return true;
        }
        catch (Exception ex)
        {
            // 触发失败，删除锁允许多次尝试
            await _redis.KeyDeleteAsync(lockKey, CommandFlags.None);
            _logger.LogError(ex, "触发器 ID「{TriggerId}」触发任务执行失败", triggerId);
            throw;
        }
    }

    /// <summary> 批量获取触发器状态 </summary>
    /// <param name="triggerIds"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<List<TriggerStatusOutput>> GetTriggerStatusListAsync(
        List<long> triggerIds,
        CancellationToken cancellationToken = default)
    {
        var triggers = await _freeSql.Select<SysJobTrigger>()
            .Where(a => !a.IsDeleted && triggerIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var results = new List<TriggerStatusOutput>();

        foreach (var trigger in triggers)
        {
            try
            {
                var triggerKey = SchedulerEntityMapper.ParseTriggerKey(trigger);
                var state = await _schedulerService.GetTriggerStateAsync(
                    triggerKey.Name,
                    triggerKey.Group,
                    cancellationToken);

                var (status, description) = ConvertTriggerState(state);
                results.Add(new TriggerStatusOutput
                {
                    Id = trigger.Id,
                    TriggerState = state,
                    TriggerStateName = status,
                    Description = description
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取触发器 ID「{TriggerId}」状态失败", trigger.Id);
                results.Add(new TriggerStatusOutput
                {
                    Id = trigger.Id,
                    TriggerState = TriggerState.None,
                    TriggerStateName = "未知",
                    Description = $"获取状态失败: {ex.Message}"
                });
            }
        }

        return results;
    }
}
