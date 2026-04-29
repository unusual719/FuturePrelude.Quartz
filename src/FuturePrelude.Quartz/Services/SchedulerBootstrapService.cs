namespace FuturePrelude.Quartz;

/// <summary> 应用启动后将业务表中的任务/触发器同步回 Quartz 调度器 </summary>
public sealed class SchedulerBootstrapService(
    IFreeSql freeSql,
    ISchedulerService schedulerService,
    ILogger<SchedulerBootstrapService> logger)
{
    private readonly IFreeSql _freeSql = freeSql;
    private readonly ISchedulerService _schedulerService = schedulerService;
    private readonly ILogger<SchedulerBootstrapService> _logger = logger;

    /// <summary> 枚举 Job 身份候选项 </summary>
    /// <param name="job"> </param>
    /// <param name="group"> </param>
    /// <returns> </returns>
    private static IEnumerable<(string Group, string Name)> EnumerateJobIdentityCandidates(SysJobDetail job, SysJobGroup group)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (SchedulerIdentityHelper.TryParseCompositeKey(job.JobKey, out var currentGroup, out var currentName)
            && seen.Add($"{currentGroup}:{currentName}"))
        {
            yield return (currentGroup, currentName);
        }

        var legacyGroup = group.Key?.Trim();
        var legacyName = job.JobName?.Trim();
        if (!string.IsNullOrWhiteSpace(legacyGroup)
            && !string.IsNullOrWhiteSpace(legacyName)
            && seen.Add($"{legacyGroup}:{legacyName}"))
        {
            yield return (legacyGroup, legacyName);
        }

        var legacyGroupName = group.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(legacyGroupName)
            && !string.IsNullOrWhiteSpace(legacyName)
            && seen.Add($"{legacyGroupName}:{legacyName}"))
        {
            yield return (legacyGroupName, legacyName);
        }
    }

    /// <summary> 枚举触发器身份候选项 </summary>
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

    /// <summary> 检查 Job 是否已注册到 Quartz Scheduler </summary>
    /// <param name="job"> </param>
    /// <param name="group"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task<bool> ContainsScheduledJobAsync(SysJobDetail job, SysJobGroup group, CancellationToken cancellationToken)
    {
        foreach (var candidate in EnumerateJobIdentityCandidates(job, group))
        {
            if (await _schedulerService.ContainsJobKey(candidate.Name, candidate.Group))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary> 检查触发器是否已注册到 Quartz Scheduler </summary>
    /// <param name="trigger"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task<bool> ContainsScheduledTriggerAsync(SysJobTrigger trigger, CancellationToken cancellationToken)
    {
        foreach (var candidate in EnumerateTriggerIdentityCandidates(trigger))
        {
            if (await _schedulerService.ContainsTriggerKey(candidate.Name, candidate.Group))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary> 应用启动后将业务表中的任务/触发器同步回 Quartz 调度器 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        /*
         * 同步流程：
         * 1. 查询所有已启用的任务及其关联的分组、配置、触发器
         * 2. 批量查询并建立字典映射（分组、HTTP配置、插件配置、触发器）
         * 3. 遍历每个任务：
         *    a. 修正分组的 GroupKey（如果变更则更新数据库）
         *    b. 修正任务的 JobKey（如果变更则更新数据库）
         *    c. 修正触发器的 TriggerKey/TriggerGroup（如果变更则更新数据库）
         *    d. 如果 Job 未注册到 Scheduler，按任务类型注册（HttpApiJob/AssemblyPluginJob）
         *    e. 如果 Job 已存在但缺少触发器，注册额外触发器
         * 4. 跳过缺少必要配置的任务并记录警告日志
         * **/

        var jobs = await _freeSql.Select<SysJobDetail>()
            .Where(a => !a.IsDeleted && a.EnableStatus == EnableStatus.Enabled)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        if (jobs.Count == 0)
        {
            return;
        }

        var groupIds = jobs.Select(a => a.GroupId).Distinct().ToArray();
        var jobIds = jobs.Select(a => a.Id).ToArray();

        var groups = await _freeSql.Select<SysJobGroup>()
            .Where(a => groupIds.Contains(a.Id) && !a.IsDeleted)
            .ToListAsync(cancellationToken);
        var httpConfigs = await _freeSql.Select<SysJobHttpConfig>()
            .Where(a => jobIds.Contains(a.JobId) && !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .ToListAsync(cancellationToken);
        var pluginConfigs = await _freeSql.Select<SysJobPlugin>()
            .Where(a => jobIds.Contains(a.JobId))
            .OrderByDescending(a => a.Id)
            .ToListAsync(cancellationToken);
        var triggers = await _freeSql.Select<SysJobTrigger>()
            .Where(a => jobIds.Contains(a.JobId) && !a.IsDeleted && a.EnableStatus == EnableStatus.Enabled)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var groupDict = groups.ToDictionary(a => a.Id);
        var httpConfigDict = new Dictionary<long, SysJobHttpConfig>();
        foreach (var config in httpConfigs)
        {
            httpConfigDict.TryAdd(config.JobId, config);
        }

        var pluginConfigDict = new Dictionary<long, SysJobPlugin>();
        foreach (var config in pluginConfigs)
        {
            pluginConfigDict.TryAdd(config.JobId, config);
        }

        var triggerDict = triggers
            .GroupBy(a => a.JobId)
            .ToDictionary(a => a.Key, a => a.ToList());

        foreach (var job in jobs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!groupDict.TryGetValue(job.GroupId, out var group))
            {
                _logger.LogWarning("启动同步跳过任务 {JobId}，未找到有效分组 {GroupId}", job.Id, job.GroupId);
                continue;
            }

            var groupChanged = group.Key != SchedulerIdentityHelper.ResolveGroupKey(group.Key, group.Name);
            SchedulerIdentityHelper.NormalizeGroupIdentity(group);
            if (groupChanged)
            {
                await _freeSql.Update<SysJobGroup>()
                    .SetSource(group)
                    .ExecuteAffrowsAsync(cancellationToken);
            }

            var originalJobKey = job.JobKey;
            SchedulerIdentityHelper.NormalizeJobIdentity(job, group.Key);
            if (!string.Equals(originalJobKey, job.JobKey, StringComparison.Ordinal))
            {
                await _freeSql.Update<SysJobDetail>()
                    .Set(a => a.JobKey, job.JobKey)
                    .Where(a => a.Id == job.Id)
                    .ExecuteAffrowsAsync(cancellationToken);
            }

            if (!triggerDict.TryGetValue(job.Id, out var jobTriggers) || jobTriggers.Count == 0)
            {
                continue;
            }

            foreach (var trigger in jobTriggers)
            {
                var originalTriggerGroup = trigger.TriggerGroup;
                var originalTriggerKey = trigger.TriggerKey;
                SchedulerIdentityHelper.NormalizeTriggerIdentity(trigger, group.Key);
                if (!string.Equals(originalTriggerGroup, trigger.TriggerGroup, StringComparison.Ordinal)
                    || !string.Equals(originalTriggerKey, trigger.TriggerKey, StringComparison.Ordinal))
                {
                    await _freeSql.Update<SysJobTrigger>()
                        .Set(a => a.TriggerGroup, trigger.TriggerGroup)
                        .Set(a => a.TriggerKey, trigger.TriggerKey)
                        .Where(a => a.Id == trigger.Id)
                        .ExecuteAffrowsAsync(cancellationToken);
                }
            }

            var jobExists = await ContainsScheduledJobAsync(job, group, cancellationToken);
            var primaryTrigger = jobTriggers[0];

            if (!jobExists)
            {
                if (job.JobType == JobType.HttpApiJob)
                {
                    if (!httpConfigDict.TryGetValue(job.Id, out var httpConfig))
                    {
                        _logger.LogWarning("启动同步跳过 HTTP 任务 {JobId}，缺少 HttpConfig", job.Id);
                        continue;
                    }

                    await _schedulerService.ScheduleHttpJobAsync(job, httpConfig, primaryTrigger, cancellationToken);
                }
                else if (job.JobType == JobType.AssemblyPluginJob)
                {
                    if (!pluginConfigDict.TryGetValue(job.Id, out var pluginConfig))
                    {
                        _logger.LogWarning("启动同步跳过插件任务 {JobId}，缺少 PluginConfig", job.Id);
                        continue;
                    }

                    await _schedulerService.SchedulePluginJobAsync(job, pluginConfig, primaryTrigger, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("启动同步跳过任务 {JobId}，不支持的任务类型 {JobType}", job.Id, job.JobType);
                    continue;
                }

                jobExists = true;
            }

            if (!jobExists)
            {
                continue;
            }

            foreach (var trigger in jobTriggers.Where(a => a.Id != primaryTrigger.Id))
            {
                if (await ContainsScheduledTriggerAsync(trigger, cancellationToken))
                {
                    continue;
                }

                await _schedulerService.ScheduleTriggerAsync(job, trigger, cancellationToken);
            }
        }
    }
}
