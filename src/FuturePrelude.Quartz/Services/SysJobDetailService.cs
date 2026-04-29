namespace FuturePrelude.Quartz.Services;

/// <summary> 任务详情服务实现 </summary>
public class SysJobDetailService(
    IFreeSql freeSql,
    ISchedulerService schedulerService,
    IDashboardNotifier dashboardNotifier) : ISysJobDetailService
{
    private readonly IFreeSql _freeSql = freeSql;
    private readonly ISchedulerService _schedulerService = schedulerService;
    private readonly IDashboardNotifier _dashboardNotifier = dashboardNotifier;

    /// <summary> 校验创建/更新任务的输入参数 </summary>
    /// <param name="jobName"> </param>
    /// <param name="jobType"> </param>
    /// <param name="httpConfig"> H </param>
    /// <param name="pluginConfig"> </param>
    /// <exception cref="ArgumentException"> </exception>
    private static void ValidateInput(
            string jobName,
            JobType jobType,
            HttpConfigInput? httpConfig,
            PluginConfigInput? pluginConfig)
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentException("任务名称不允许为空");

        if (jobType == JobType.HttpApiJob && httpConfig == null)
            throw new ArgumentException("HTTP 任务必须配置 HttpConfig");

        if (jobType == JobType.AssemblyPluginJob && pluginConfig == null)
            throw new ArgumentException("插件任务必须配置 PluginConfig");
    }

    /// <summary> 校验 HTTP 配置的请求地址是否合法（必须是 HTTP 或 HTTPS 协议的绝对 URL） </summary>
    /// <param name="httpConfig"> </param>
    /// <exception cref="ArgumentException"> </exception>
    private static void ValidateHttpConfig(HttpConfigInput? httpConfig)
    {
        if (httpConfig == null)
            return;

        bool isValid = Uri.TryCreate(httpConfig.RequestUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!isValid)
            throw new ArgumentException($"「{httpConfig.RequestUrl}」非法请求地址");

        if (httpConfig.AuthType == AuthType.BearerToken)
        {
            var bearerTokenRuntimeConfig = HttpBearerTokenRuntime.DeserializeConfig(httpConfig.AuthCredentials);
            bearerTokenRuntimeConfig.Validate();
        }
    }

    /// <summary> 根据 Quartz JobRuntimeState 转换为持久化的 JobStatus </summary>
    /// <param name="jobState"> </param>
    /// <returns> </returns>
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
            global::Quartz.TriggerState.None => JobStatus.NoTrigger,
            global::Quartz.TriggerState.Complete => JobStatus.NoTrigger,
            global::Quartz.TriggerState.Error => JobStatus.Error,
            _ => JobStatus.Idle
        };
    }

    /// <summary> 根据启用状态和运行时状态解析更新后的 JobStatus </summary>
    /// <param name="enableStatus"> </param>
    /// <param name="currentState"> </param>
    /// <param name="hasEnabledTriggers"> </param>
    /// <returns> </returns>
    private static JobStatus ResolveUpdatedJobStatus(
        EnableStatus enableStatus,
        JobRuntimeState currentState,
        bool hasEnabledTriggers)
    {
        if (enableStatus == EnableStatus.Disabled)
        {
            return JobStatus.Paused;
        }

        if (!hasEnabledTriggers)
        {
            return JobStatus.NoTrigger;
        }

        if (currentState.Exists && currentState.IsPaused)
        {
            return JobStatus.Paused;
        }

        return JobStatus.Idle;
    }

    /// <summary> 批量修正触发器身份（TriggerKey/TriggerGroup） </summary>
    /// <param name="triggers"> </param>
    /// <param name="groupKey"> </param>
    private static void NormalizeTriggerIdentities(IReadOnlyList<SysJobTrigger> triggers, string groupKey)
    {
        foreach (var trigger in triggers)
        {
            SchedulerIdentityHelper.NormalizeTriggerIdentity(trigger, groupKey);
        }
    }

    /// <summary> 重建 Quartz Scheduler 中的 Job 和触发器 </summary>
    /// <param name="job"> </param>
    /// <param name="previousGroup"> </param>
    /// <param name="previousName"> </param>
    /// <param name="previousJobExists"> </param>
    /// <param name="triggers"> </param>
    /// <param name="httpConfig"> </param>
    /// <param name="pluginConfig"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    /// <exception cref="InvalidOperationException"> </exception>
    /// <exception cref="NotSupportedException"> </exception>
    private async Task RebuildSchedulerJobAsync(
        SysJobDetail job,
        string previousGroup,
        string previousName,
        bool previousJobExists,
        IReadOnlyList<SysJobTrigger> triggers,
        SysJobHttpConfig? httpConfig,
        SysJobPlugin? pluginConfig,
        CancellationToken cancellationToken)
    {
        /*
         * 业务逻辑：
         * 1. 如果 previousJobExists=true，先从 Scheduler 中删除旧 Job
         * 2. 如果 Job 未启用或没有启用的触发器，直接返回
         * 3. 按任务类型注册新 Job（HttpApiJob/AssemblyPluginJob）
         * 4. 注册额外的触发器（除主触发器外）
         * **/
        if (previousJobExists)
        {
            await _schedulerService.PauseJobAsync(previousGroup, previousName, cancellationToken);
            await _schedulerService.DeleteJobAsync(previousGroup, previousName, cancellationToken);
        }

        var activeTriggers = triggers
            .Where(a => !a.IsDeleted && a.EnableStatus == EnableStatus.Enabled)
            .OrderBy(a => a.Id)
            .ToList();

        if (job.EnableStatus != EnableStatus.Enabled || activeTriggers.Count == 0)
        {
            return;
        }

        var primaryTrigger = activeTriggers[0];

        if (job.JobType == JobType.HttpApiJob)
        {
            if (httpConfig == null)
                throw new InvalidOperationException($"任务 ID「{job.Id}」缺少 HttpConfig 配置");

            await _schedulerService.ScheduleHttpJobAsync(job, httpConfig, primaryTrigger, cancellationToken);
        }
        else if (job.JobType == JobType.AssemblyPluginJob)
        {
            if (pluginConfig == null)
                throw new InvalidOperationException($"任务 ID「{job.Id}」缺少 PluginConfig 配置");

            await _schedulerService.SchedulePluginJobAsync(job, pluginConfig, primaryTrigger, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"任务类型「{job.JobType}」暂不支持调度");
        }

        foreach (var trigger in activeTriggers.Where(a => a.Id != primaryTrigger.Id))
        {
            await _schedulerService.ScheduleTriggerAsync(job, trigger, cancellationToken);
        }
    }

    /// <summary> 确保已启用的 Job 注册到 Quartz Scheduler </summary>
    /// <param name="job"> </param>
    /// <param name="jobGroup"> </param>
    /// <param name="jobName"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task EnsureEnabledJobScheduledAsync(
        SysJobDetail job,
        string jobGroup,
        string jobName,
        CancellationToken cancellationToken)
    {
        /*
         * 业务逻辑：
         *  1. 查询 Job 下所有触发器
         *  2. 如果 Job 未启用或没有启用的触发器，直接返回
         *  3. 根据任务类型查询配置（HttpConfig/PluginConfig）
         *  4. 调用 RebuildSchedulerJobAsync 完成注册
         * **/
        var triggers = await _freeSql.Select<SysJobTrigger>()
            .Where(a => a.JobId == job.Id && !a.IsDeleted)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        if (job.EnableStatus != EnableStatus.Enabled
            || !triggers.Any(a => a.EnableStatus == EnableStatus.Enabled))
        {
            return;
        }

        SysJobHttpConfig? httpConfig = null;
        SysJobPlugin? pluginConfig = null;

        if (job.JobType == JobType.HttpApiJob)
        {
            httpConfig = await _freeSql.Select<SysJobHttpConfig>()
                .Where(a => a.JobId == job.Id && !a.IsDeleted)
                .OrderByDescending(a => a.Id)
                .FirstAsync(cancellationToken);
        }
        else if (job.JobType == JobType.AssemblyPluginJob)
        {
            pluginConfig = await _freeSql.Select<SysJobPlugin>()
                .Where(a => a.JobId == job.Id)
                .OrderByDescending(a => a.Id)
                .FirstAsync(cancellationToken);
        }

        await RebuildSchedulerJobAsync(
            job,
            jobGroup,
            jobName,
            previousJobExists: false,
            triggers,
            httpConfig,
            pluginConfig,
            cancellationToken);
    }

    /// <summary> 批量构建任务详情输出列表 </summary>
    /// <param name="entities"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task<List<SysJobDetailOutput>> BuildOutputsAsync(
        IReadOnlyList<SysJobDetail> entities,
        CancellationToken cancellationToken)
    {
        if (entities.Count == 0)
        {
            return [];
        }

        var groupIds = entities.Select(a => a.GroupId).Distinct().ToArray();
        var jobIds = entities.Select(a => a.Id).ToArray();

        var groups = await _freeSql.Select<SysJobGroup>()
            .Where(a => groupIds.Contains(a.Id))
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
            .Where(a => jobIds.Contains(a.JobId) && !a.IsDeleted)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        var groupDict = groups.ToDictionary(a => a.Id, a => a);
        var httpConfigDict = new Dictionary<long, SysJobHttpConfig>();
        foreach (var httpConfig in httpConfigs)
        {
            httpConfigDict.TryAdd(httpConfig.JobId, httpConfig);
        }

        var pluginConfigDict = new Dictionary<long, SysJobPlugin>();
        foreach (var pluginConfig in pluginConfigs)
        {
            pluginConfigDict.TryAdd(pluginConfig.JobId, pluginConfig);
        }

        var triggerDict = triggers
            .GroupBy(a => a.JobId)
            .ToDictionary(
                group => group.Key,
                group => group.Adapt<List<TriggerOutput>>());

        var outputs = new List<SysJobDetailOutput>(entities.Count);
        foreach (var entity in entities)
        {
            var output = entity.Adapt<SysJobDetailOutput>();
            output.JobStatus = entity.JobStatus;

            if (groupDict.TryGetValue(entity.GroupId, out var group))
            {
                output.JobGroup = group.Adapt<SysJobGroupOutput>();
            }

            if (httpConfigDict.TryGetValue(entity.Id, out var httpConfig))
            {
                output.HttpConfig = httpConfig.Adapt<HttpConfigOutput>();
                output.HttpConfig.AuthCredentials = httpConfig.AuthCredentials;
            }

            if (pluginConfigDict.TryGetValue(entity.Id, out var pluginConfig))
            {
                output.PluginConfig = pluginConfig.Adapt<PluginConfigOutput>();
            }

            if (triggerDict.TryGetValue(entity.Id, out var jobTriggers))
            {
                output.Triggers = jobTriggers;
            }

            outputs.Add(output);
        }

        return outputs;
    }

    /// <summary> 分页获取任务详情列表 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<PageResponse<SysJobDetailOutput>> GetPageListAsync(
        SysJobDetailQueryInput query, CancellationToken cancellationToken = default)
    {
        var select = _freeSql.Select<SysJobDetail>()
            .Where(a => !a.IsDeleted);

        if (query.GroupId.HasValue)
            select = select.Where(a => a.GroupId == query.GroupId.Value);

        if (query.JobType.HasValue)
        {
            var jobType = (JobType)query.JobType.Value;
            select = select.Where(a => a.JobType == jobType);
        }

        if (query.EnableStatus.HasValue)
            select = select.Where(a => a.EnableStatus == query.EnableStatus.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            select = select.Where(a => a.JobName.Contains(query.Keyword));

        var totalCount = await select.CountAsync(cancellationToken);

        var entities = await select
            .OrderByDescending(a => a.CreateTime)
            .Page(query.PageIndex, query.PageSize)
            .ToListAsync(cancellationToken);

        var items = await BuildOutputsAsync(entities, cancellationToken);

        return new PageResponse<SysJobDetailOutput>
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    /// <summary> 根据ID获取任务详情 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<SysJobDetailOutput?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var outputs = await BuildOutputsAsync([entity], cancellationToken);
        return outputs.FirstOrDefault();
    }

    /// <summary> 创建任务详情 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<SysJobDetailOutput> CreateAsync(SysJobDetailCreateInput input, CancellationToken cancellationToken = default)
    {
        ValidateInput(input.JobName, input.JobType, input.HttpConfig, input.PluginConfig);
        ValidateHttpConfig(input.HttpConfig);

        // 校验分组是否存在
        var group = await _freeSql.Select<SysJobGroup>()
            .Where(a => a.Id == input.GroupId && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (group == null)
            throw new KeyNotFoundException($"分组 ID「{input.GroupId}」不存在");

        // 校验 JobName 不重复
        var exists = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.GroupId == input.GroupId && a.JobName == input.JobName.Trim() && !a.IsDeleted)
            .AnyAsync(cancellationToken);
        if (exists)
            throw new ArgumentException($"分组下任务名称「{input.JobName}」已存在");

        var entity = input.Adapt<SysJobDetail>();
        entity.GroupId = group.Id;
        entity.CreateBy = "system";
        entity.CreateTime = ChinaTimeZoneConverter.Now();
        entity.EnableStatus = EnableStatus.Enabled;
        entity.JobStatus = JobStatus.Idle;
        entity.LastRunStatus = RunStatus.Pending;
        entity.RetryOnFailure = input.RetryOnFailure;
        entity.MaxRetry = input.RetryOnFailure ? input.MaxRetry : null;
        entity.RetryBackoffSeconds = input.RetryOnFailure ? input.RetryBackoffSeconds : null;
        SchedulerIdentityHelper.NormalizeJobIdentity(entity, group.Key);

        using var uow = _freeSql.CreateUnitOfWork();
        var orm = uow.Orm;

        // JobId
        entity.Id = (long)await orm.Insert(entity).ExecuteIdentityAsync(cancellationToken);

        // 根据 JobType 插入关联配置
        if (input.JobType == JobType.HttpApiJob && input.HttpConfig != null)
        {
            // Http
            var httpConfig = input.HttpConfig.Adapt<SysJobHttpConfig>();

            httpConfig.JobId = entity.Id;
            httpConfig.CreateBy = "system";
            httpConfig.CreateTime = ChinaTimeZoneConverter.Now();
            httpConfig.Id = (long)await orm.Insert(httpConfig).ExecuteIdentityAsync(cancellationToken);
        }
        else if (input.JobType == JobType.AssemblyPluginJob && input.PluginConfig != null)
        {
            // AssemblyPlugin
            var pluginConfig = input.PluginConfig.Adapt<SysJobPlugin>();
            pluginConfig.JobId = entity.Id;
            pluginConfig.CreateBy = "system";
            pluginConfig.CreateTime = ChinaTimeZoneConverter.Now();
            pluginConfig.Id = (long)await orm.Insert(pluginConfig).ExecuteIdentityAsync(cancellationToken);
        }

        uow.Commit();

        // 查询返回完整数据
        var output = await GetByIdAsync(entity.Id, cancellationToken) ?? throw new Exception("创建失败");
        await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        return output;
    }

    /// <summary> 更新任务详情 </summary>
    /// <param name="input"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<SysJobDetailOutput> UpdateAsync(SysJobDetailUpdateInput input, CancellationToken cancellationToken = default)
    {
        ValidateInput(input.JobName, input.JobType, input.HttpConfig, input.PluginConfig);
        ValidateHttpConfig(input.HttpConfig);

        var entity = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == input.Id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (entity == null)
            throw new KeyNotFoundException($"任务 ID「{input.Id}」不存在");

        // 校验 JobName 不与其他记录冲突
        var normalizedJobName = input.JobName.Trim();
        var nameExists = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.GroupId == input.GroupId && a.JobName == normalizedJobName && a.Id != input.Id && !a.IsDeleted)
            .AnyAsync(cancellationToken);

        if (nameExists)
            throw new ArgumentException($"分组下任务名称「{input.JobName}」已存在");

        // 获取分组
        var group = await _freeSql.Select<SysJobGroup>()
            .Where(a => a.Id == input.GroupId && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (group == null)
            throw new KeyNotFoundException($"分组 ID「{input.GroupId}」不存在");

        var (currentGroup, currentName) = SchedulerEntityMapper.ParseJobKey(entity);
        var jobState = await _schedulerService.GetJobStateAsync(currentGroup, currentName, cancellationToken);
        if (jobState.Exists && !jobState.IsPaused)
            throw new InvalidOperationException("当前任务非处于暂停状态，请先将任务进行暂停后再进行修改");

        var triggers = await _freeSql.Select<SysJobTrigger>()
            .Where(a => a.JobId == entity.Id && !a.IsDeleted)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        NormalizeTriggerIdentities(triggers, group.Key);

        var hasEnabledTriggers = triggers.Any(a => a.EnableStatus == EnableStatus.Enabled);

        using var uow = _freeSql.CreateUnitOfWork();
        var orm = uow.Orm;

        // 更新基础字段
        var previousJobType = entity.JobType;
        entity.GroupId = input.GroupId;
        entity.JobName = normalizedJobName;
        entity.JobType = input.JobType;
        entity.Description = input.Description;
        entity.DisallowConcurrent = input.DisallowConcurrent;
        entity.RetryOnFailure = input.RetryOnFailure;
        entity.MaxRetry = input.RetryOnFailure ? input.MaxRetry : null;
        entity.RetryBackoffSeconds = input.RetryOnFailure ? input.RetryBackoffSeconds : null;
        entity.JobStatus = ResolveUpdatedJobStatus(entity.EnableStatus, jobState, hasEnabledTriggers);
        entity.UpdateBy = "system";
        entity.UpdateTime = ChinaTimeZoneConverter.Now();
        SchedulerIdentityHelper.NormalizeJobIdentity(entity, group.Key);

        SysJobHttpConfig? scheduledHttpConfig = null;
        SysJobPlugin? scheduledPluginConfig = null;

        // 更新 SysJobDetail
        await orm.Update<SysJobDetail>()
            .SetSource(entity)
            .ExecuteAffrowsAsync(cancellationToken);

        if (previousJobType == JobType.HttpApiJob)
        {
            // Http
            await orm.Delete<SysJobHttpConfig>()
                .Where(a => a.JobId == entity.Id)
                .ExecuteAffrowsAsync(cancellationToken);
        }

        if (previousJobType == JobType.AssemblyPluginJob)
        {
            // AssemblyPluginJob
            await orm.Delete<SysJobPlugin>()
                .Where(a => a.JobId == entity.Id)
                .ExecuteAffrowsAsync(cancellationToken);
        }

        // 更新关联配置
        if (input.JobType == JobType.HttpApiJob && input.HttpConfig != null)
        {
            // 新增 HttpApiJob
            var httpConfig = input.HttpConfig.Adapt<SysJobHttpConfig>();

            httpConfig.JobId = entity.Id;
            httpConfig.CreateBy = "system";
            httpConfig.CreateTime = ChinaTimeZoneConverter.Now();
            httpConfig.Id = (long)await orm.Insert(httpConfig).ExecuteIdentityAsync(cancellationToken);
            scheduledHttpConfig = httpConfig;
        }
        else if (input.JobType == JobType.AssemblyPluginJob && input.PluginConfig != null)
        {
            // 新增 AssemblyPluginJob
            var pluginConfig = input.PluginConfig.Adapt<SysJobPlugin>();
            pluginConfig.JobId = entity.Id;
            pluginConfig.CreateBy = "system";
            pluginConfig.CreateTime = ChinaTimeZoneConverter.Now();
            pluginConfig.Id = (long)await orm.Insert(pluginConfig).ExecuteIdentityAsync(cancellationToken);
            scheduledPluginConfig = pluginConfig;
        }

        foreach (var trigger in triggers)
        {
            await orm.Update<SysJobTrigger>()
                .Set(a => a.TriggerGroup, trigger.TriggerGroup)
                .Set(a => a.TriggerKey, trigger.TriggerKey)
                .Where(a => a.Id == trigger.Id)
                .ExecuteAffrowsAsync(cancellationToken);
        }

        uow.Commit();

        await RebuildSchedulerJobAsync(
            entity,
            currentGroup,
            currentName,
            jobState.Exists,
            triggers,
            scheduledHttpConfig,
            scheduledPluginConfig,
            cancellationToken);

        var output = await GetByIdAsync(entity.Id, cancellationToken) ?? throw new Exception("更新失败");
        await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        return output;
    }

    /// <summary> 删除任务详情（真删除） </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (entity == null)
            throw new KeyNotFoundException($"任务 ID「{id}」不存在");

        var (jobGroup, jobName) = SchedulerEntityMapper.ParseJobKey(entity);

        using var uow = _freeSql.CreateUnitOfWork();
        var orm = uow.Orm;

        if (entity.JobType == JobType.HttpApiJob)
        {
            // Http
            await orm.Delete<SysJobHttpConfig>()
                .Where(a => a.JobId == entity.Id)
                .ExecuteAffrowsAsync(cancellationToken);
        }

        if (entity.JobType == JobType.AssemblyPluginJob)
        {
            // AssemblyPluginJob
            await orm.Delete<SysJobPlugin>()
                .Where(a => a.JobId == entity.Id)
                .ExecuteAffrowsAsync(cancellationToken);
        }

        await orm.Delete<SysJobTrigger>()
            .Where(a => a.JobId == entity.Id)
            .ExecuteAffrowsAsync(cancellationToken);

        // 真删除不做假删除
        var rows = await orm.Delete<SysJobDetail>()
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        // 任务暂停和删除
        await _schedulerService.PauseJobAsync(jobGroup, jobName, cancellationToken);
        await _schedulerService.DeleteJobAsync(jobGroup, jobName, cancellationToken);

        uow.Commit();

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 启用任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> EnableAsync(long id, CancellationToken cancellationToken = default)
    {
        var jobInfo = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (jobInfo == null)
            throw new KeyNotFoundException($"任务 ID「{id}」不存在");

        var (group, name) = SchedulerEntityMapper.ParseJobKey(jobInfo);
        jobInfo.EnableStatus = EnableStatus.Enabled;

        var currentState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);
        if (currentState.Exists)
        {
            await _schedulerService.ResumeJobAsync(group, name, cancellationToken);
        }
        else
        {
            await EnsureEnabledJobScheduledAsync(jobInfo, group, name, cancellationToken);
        }

        var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);

        var rows = await _freeSql.Update<SysJobDetail>()
            .Set(a => a.EnableStatus, EnableStatus.Enabled)
            .Set(a => a.JobStatus, ResolvePersistedJobStatus(jobState))
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 禁用任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> DisableAsync(long id, CancellationToken cancellationToken = default)
    {
        var jobInfo = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (jobInfo == null)
            throw new KeyNotFoundException($"任务 ID「{id}」不存在");

        // 暂停任务
        var (group, name) = SchedulerEntityMapper.ParseJobKey(jobInfo);
        await _schedulerService.PauseJobAsync(group, name, cancellationToken);

        var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);

        var rows = await _freeSql.Update<SysJobDetail>()
            .Set(a => a.EnableStatus, EnableStatus.Disabled)
            .Set(a => a.JobStatus, ResolvePersistedJobStatus(jobState))
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 暂停任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> PauseJobAsync(long id, CancellationToken cancellationToken = default)
    {
        var jobInfo = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.Id == id && !a.IsDeleted)
            .FirstAsync(cancellationToken);

        if (jobInfo == null)
            throw new KeyNotFoundException($"任务 ID「{id}」不存在");

        // 暂停任务
        var (group, name) = SchedulerEntityMapper.ParseJobKey(jobInfo);
        await _schedulerService.PauseJobAsync(group, name, cancellationToken);

        var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);

        var rows = await _freeSql.Update<SysJobDetail>()
            .Set(a => a.JobStatus, ResolvePersistedJobStatus(jobState))
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }

    /// <summary> 恢复任务 </summary>
    /// <param name="id"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<bool> ResumeJobAsync(long id, CancellationToken cancellationToken = default)
    {
        var jobInfo = await _freeSql.Select<SysJobDetail>()
        .Where(a => a.Id == id && !a.IsDeleted)
        .FirstAsync(cancellationToken);

        if (jobInfo == null)
            throw new KeyNotFoundException($"任务 ID「{id}」不存在");

        var (group, name) = SchedulerEntityMapper.ParseJobKey(jobInfo);
        var currentState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);
        if (currentState.Exists)
        {
            await _schedulerService.ResumeJobAsync(group, name, cancellationToken);
        }
        else
        {
            await EnsureEnabledJobScheduledAsync(jobInfo, group, name, cancellationToken);
        }

        var jobState = await _schedulerService.GetJobStateAsync(group, name, cancellationToken);

        var rows = await _freeSql.Update<SysJobDetail>()
            .Set(a => a.JobStatus, ResolvePersistedJobStatus(jobState))
            .Where(a => a.Id == id)
            .ExecuteAffrowsAsync(cancellationToken);

        if (rows > 0)
        {
            await _dashboardNotifier.NotifyOverviewChangedAsync(cancellationToken);
        }

        return rows > 0;
    }
}
