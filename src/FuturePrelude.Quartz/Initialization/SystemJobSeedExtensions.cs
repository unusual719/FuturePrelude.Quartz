using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace FuturePrelude.Quartz;

/// <summary> 系统内置 Job 种子初始化扩展 </summary>
public static class SystemJobSeedExtensions
{
    private const string SeedOperator = "system";

    private static async Task<string?> ResolveHealthyBaseUrlAsync(
            IServerAddressesFeature addressesFeature,
            IHttpClientFactory httpClientFactory,
            Microsoft.Extensions.Logging.ILogger? logger,
            CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(InternalConstants.HttpClientIgnoreVerifySsl);

        foreach (var address in addressesFeature.Addresses)
        {
            var baseUrl = address?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                continue;
            }

            logger?.LogInformation("当前服务运行地址: {Address}", baseUrl);

            try
            {
                using var response = await httpClient.GetAsync($"{baseUrl}/health", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    logger?.LogInformation("系统内置 Job 使用健康地址: {Address}", baseUrl);
                    return baseUrl;
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "健康检查地址不可用：{Address}", baseUrl);
            }
        }

        return null;
    }

    private static async Task<SysJobGroup> UpsertSystemGroupAsync(IFreeSql freeSql, CancellationToken cancellationToken)
    {
        var group = await freeSql.Select<SysJobGroup>()
            .Where(x => x.Key == SystemJobSeedDefinitions.SystemGroupKey || x.Name == SystemJobSeedDefinitions.SystemGroupName)
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);

        if (group == null)
        {
            group = new SysJobGroup
            {
                Name = SystemJobSeedDefinitions.SystemGroupName,
                Key = SystemJobSeedDefinitions.SystemGroupKey,
                ICon = SystemJobSeedDefinitions.SystemGroupIcon,
                EnableStatus = EnableStatus.Enabled,
                Description = SystemJobSeedDefinitions.SystemGroupDescription,
                SortOrder = -1,
                CreateBy = SeedOperator,
                CreateTime = ChinaTimeZoneConverter.Now()
            };
            SchedulerIdentityHelper.NormalizeGroupIdentity(group);
            group.Id = (long)await freeSql.Insert(group).ExecuteIdentityAsync(cancellationToken);
            return group;
        }

        group.Name = SystemJobSeedDefinitions.SystemGroupName;
        group.Key = SystemJobSeedDefinitions.SystemGroupKey;
        group.ICon = SystemJobSeedDefinitions.SystemGroupIcon;
        group.EnableStatus = EnableStatus.Enabled;
        group.IsDeleted = false;
        group.Description = SystemJobSeedDefinitions.SystemGroupDescription;
        group.UpdateBy = SeedOperator;
        group.UpdateTime = ChinaTimeZoneConverter.Now();
        SchedulerIdentityHelper.NormalizeGroupIdentity(group);

        await freeSql.Update<SysJobGroup>()
            .SetSource(group)
            .IgnoreColumns(x => new { x.CreateBy, x.CreateTime })
            .ExecuteAffrowsAsync(cancellationToken);

        return group;
    }

    private static async Task<SysJobDetail> UpsertSystemJobAsync(
            IFreeSql freeSql,
            SysJobGroup group,
            SystemHttpJobSeed seed,
            CancellationToken cancellationToken)
    {
        var expectedJobKey = SchedulerIdentityHelper.BuildCompositeKey(group.Key, seed.JobName.ToMd5());
        var job = await freeSql.Select<SysJobDetail>()
            .Where(x => x.JobKey == expectedJobKey || (x.GroupId == group.Id && x.JobName == seed.JobName))
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);

        if (job == null)
        {
            job = new SysJobDetail
            {
                GroupId = group.Id,
                JobName = seed.JobName,
                JobType = JobType.HttpApiJob,
                DisallowConcurrent = true,
                RetryOnFailure = false,
                EnableStatus = EnableStatus.Enabled,
                JobStatus = JobStatus.Idle,
                Description = seed.JobDescription,
                CreateBy = SeedOperator,
                CreateTime = ChinaTimeZoneConverter.Now()
            };
            SchedulerIdentityHelper.NormalizeJobIdentity(job, group.Key);
            job.Id = (long)await freeSql.Insert(job).ExecuteIdentityAsync(cancellationToken);
            return job;
        }

        job.GroupId = group.Id;
        job.JobName = seed.JobName;
        job.JobType = JobType.HttpApiJob;
        job.DisallowConcurrent = true;
        job.RetryOnFailure = false;
        job.EnableStatus = EnableStatus.Enabled;
        job.JobStatus = JobStatus.Idle;
        job.IsDeleted = false;
        job.Description = seed.JobDescription;
        job.UpdateBy = SeedOperator;
        job.UpdateTime = ChinaTimeZoneConverter.Now();
        SchedulerIdentityHelper.NormalizeJobIdentity(job, group.Key);

        await freeSql.Update<SysJobDetail>()
            .SetSource(job)
            .IgnoreColumns(x => new { x.CreateBy, x.CreateTime })
            .ExecuteAffrowsAsync(cancellationToken);

        return job;
    }

    private static async Task<SysJobHttpConfig> UpsertSystemHttpConfigAsync(
            IFreeSql freeSql,
            SysJobDetail job,
            string baseUrl,
            SystemHttpJobSeed seed,
            CancellationToken cancellationToken)
    {
        var requestUrl = $"{baseUrl.TrimEnd('/')}{seed.RelativeUrl}";
        var httpConfig = await freeSql.Select<SysJobHttpConfig>()
            .Where(x => x.JobId == job.Id)
            .OrderByDescending(x => x.Id)
            .FirstAsync(cancellationToken);

        if (httpConfig == null)
        {
            httpConfig = new SysJobHttpConfig
            {
                JobId = job.Id,
                RequestMethod = "POST",
                RequestUrl = requestUrl,
                RequestHeaders = "{}",
                RequestBody = null,
                ContentType = System.Net.Mime.MediaTypeNames.Application.Json,
                TimeoutSeconds = seed.TimeoutSeconds,
                AuthType = AuthType.None,
                AuthCredentials = null,
                IsDeleted = false,
                CreateBy = SeedOperator,
                CreateTime = ChinaTimeZoneConverter.Now()
            };
            httpConfig.Id = (long)await freeSql.Insert(httpConfig).ExecuteIdentityAsync(cancellationToken);
            return httpConfig;
        }

        httpConfig.RequestMethod = "POST";
        httpConfig.RequestUrl = requestUrl;
        httpConfig.RequestHeaders = "{}";
        httpConfig.RequestBody = null;
        httpConfig.ContentType = System.Net.Mime.MediaTypeNames.Application.Json;
        httpConfig.TimeoutSeconds = seed.TimeoutSeconds;
        httpConfig.AuthType = AuthType.None;
        httpConfig.AuthCredentials = null;
        httpConfig.IsDeleted = false;

        await freeSql.Update<SysJobHttpConfig>()
            .SetSource(httpConfig)
            .IgnoreColumns(x => new { x.CreateBy, x.CreateTime })
            .ExecuteAffrowsAsync(cancellationToken);

        return httpConfig;
    }

    private static async Task<SysJobTrigger> UpsertSystemTriggerAsync(
            IFreeSql freeSql,
            SysJobDetail job,
            SysJobGroup group,
            SystemHttpJobSeed seed,
            CancellationToken cancellationToken)
    {
        var expectedTriggerKey = SchedulerIdentityHelper.BuildCompositeKey(group.Key, seed.TriggerName.ToMd5());
        var trigger = await freeSql.Select<SysJobTrigger>()
            .Where(x => x.TriggerKey == expectedTriggerKey || (x.JobId == job.Id && x.TriggerName == seed.TriggerName))
            .OrderBy(x => x.Id)
            .FirstAsync(cancellationToken);

        if (trigger == null)
        {
            trigger = new SysJobTrigger
            {
                JobId = job.Id,
                TriggerName = seed.TriggerName,
                TriggerGroup = group.Key,
                TriggerType = TriggerType.Cron,
                EnableStatus = EnableStatus.Enabled,
                TriggerState = global::Quartz.TriggerState.Normal,
                Description = seed.TriggerDescription,
                CreateBy = SeedOperator,
                CreateTime = ChinaTimeZoneConverter.Now()
            };
            TriggerConfigJsonHelper.NormalizeForSave(trigger, seed.CronExpression, seed.CronDescription, seed.TimeZoneId);
            trigger.Id = (long)await freeSql.Insert(trigger).ExecuteIdentityAsync(cancellationToken);
            return trigger;
        }

        trigger.JobId = job.Id;
        trigger.TriggerName = seed.TriggerName;
        trigger.TriggerGroup = group.Key;
        trigger.TriggerType = TriggerType.Cron;
        trigger.EnableStatus = EnableStatus.Enabled;
        trigger.TriggerState = global::Quartz.TriggerState.Normal;
        trigger.IsDeleted = false;
        trigger.Description = seed.TriggerDescription;
        trigger.UpdateBy = SeedOperator;
        trigger.UpdateTime = ChinaTimeZoneConverter.Now();
        TriggerConfigJsonHelper.NormalizeForSave(trigger, seed.CronExpression, seed.CronDescription, seed.TimeZoneId);

        await freeSql.Update<SysJobTrigger>()
            .SetSource(trigger)
            .IgnoreColumns(x => new { x.CreateBy, x.CreateTime })
            .ExecuteAffrowsAsync(cancellationToken);

        return trigger;
    }

    /// <summary> 应用启动后初始化系统内置 Job </summary>
    /// <param name="app"> </param>
    public static void SeedSystemJobsOnStarted(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await app.Services.SeedSystemJobsAsync(app.Logger);
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "系统内置 Job 种子初始化失败");
                }
            });
        });
    }

    /// <summary> 初始化系统内置 Job </summary>
    /// <param name="services"> </param>
    /// <param name="logger"> </param>
    /// <param name="cancellationToken"> </param>
    public static async Task SeedSystemJobsAsync(
        this IServiceProvider services,
        Microsoft.Extensions.Logging.ILogger? logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var freeSql = serviceProvider.GetService<IFreeSql>();
        var schedulerService = serviceProvider.GetService<ISchedulerService>();
        var server = serviceProvider.GetService<IServer>();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

        if (freeSql == null || schedulerService == null || server == null || httpClientFactory == null)
        {
            logger?.LogDebug("系统内置 Job 种子依赖缺失，跳过初始化");
            return;
        }

        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        if (addressesFeature == null || addressesFeature.Addresses.Count == 0)
        {
            logger?.LogDebug("未获取到服务监听地址，跳过系统内置 Job 初始化");
            return;
        }

        var baseUrl = await ResolveHealthyBaseUrlAsync(addressesFeature, httpClientFactory, logger, cancellationToken);
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger?.LogWarning("未找到可用的服务健康地址，跳过系统内置 Job 初始化");
            return;
        }

        var group = await UpsertSystemGroupAsync(freeSql, cancellationToken);
        var job = await UpsertSystemJobAsync(freeSql, group, SystemJobSeedDefinitions.LogCleanup, cancellationToken);
        var httpConfig = await UpsertSystemHttpConfigAsync(freeSql, job, baseUrl, SystemJobSeedDefinitions.LogCleanup, cancellationToken);
        var trigger = await UpsertSystemTriggerAsync(freeSql, job, group, SystemJobSeedDefinitions.LogCleanup, cancellationToken);

        if (!SchedulerIdentityHelper.TryParseCompositeKey(job.JobKey, out var groupKey, out var jobNameCode))
        {
            logger?.LogWarning("系统内置 JobKey 无效，跳过调度：{JobKey}", job.JobKey);
            return;
        }

        if (await schedulerService.ContainsJobKey(jobNameCode, groupKey))
        {
            logger?.LogDebug("系统内置 Job 已存在于 Quartz，跳过重复调度：{JobKey}", job.JobKey);
            return;
        }

        await schedulerService.ScheduleHttpJobAsync(job, httpConfig, trigger, cancellationToken);
        logger?.LogInformation("已完成系统内置 Job 初始化：{JobKey}", job.JobKey);
    }
}