namespace FuturePrelude.Quartz;

/// <summary> 通用插件C# 程序集执行 Job（基于反射） </summary>
public class AssemblyPluginJob : IJob
{
    private readonly ILogger<AssemblyPluginJob> _logger;
    private readonly IJobStorageService _jobStorage;
    private readonly IDistributedJobLockService _distributedJobLockService;

    public AssemblyPluginJob(ILogger<AssemblyPluginJob> logger
        , IJobStorageService jobStorage
        , IDistributedJobLockService distributedJobLockService)
    {
        _logger = logger;
        _jobStorage = jobStorage;
        _distributedJobLockService = distributedJobLockService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        DistributedJobLockAcquireResult? lockResult = null;

        try
        {
            var jobName = context.JobDetail.Key.Name;
            var jobGroupName = context.JobDetail.Key.Group;
            var triggerName = context.Trigger.Key.Name;
            var triggerGroup = context.Trigger.Key.Group;

            var jobInfo = await _jobStorage.GetJobAsync(jobName, jobGroupName, context.CancellationToken);
            if (jobInfo == null)
            {
                _logger.LogError("[{runInstanceId}] 作业信息不存在（{jobGroup}.{jobName}），任务终止", context.FireInstanceId, jobGroupName, jobName);
                throw new JobExecutionException("作业信息不存在，无法运行插件任务");
            }

            var triggerKey = JobExecutionRuntime.BuildTriggerKey(triggerName, triggerGroup);
            var currentTrigger = JobExecutionRuntime.ResolveCurrentTrigger(jobInfo, triggerName, triggerGroup);
            var jobDataMap = context.MergedJobDataMap;
            var expiration = JobExecutionRuntime.ResolveExpiration(jobDataMap, currentTrigger);
            if (JobExecutionRuntime.IsExpired(jobDataMap, currentTrigger, DateTimeOffset.UtcNow))
            {
                _logger.LogError(
                    "[{runInstanceId}] 任务已过期（{endTime}），任务终止",
                    context.FireInstanceId,
                    expiration.HasValue
                        ? ChinaTimeZoneConverter.FromOffset(expiration.Value).ToString("yyyy-MM-dd HH:mm:ss")
                        : null);
                await context.Scheduler.PauseTrigger(new TriggerKey(context.Trigger.Key.Name, context.Trigger.Key.Group));
                context.SetIsSuccess(false);
                return;
            }

            // 是否禁止并发执行
            if (jobInfo.DisallowConcurrent)
            {
                try
                {
                    lockResult = await _distributedJobLockService.TryAcquireAsync(
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        context.FireInstanceId,
                        context.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[{runInstanceId}] 获取分布式锁时发生异常，跳过执行插件任务 Job={jobKey} Trigger={triggerKey}",
                        context.FireInstanceId,
                        context.JobDetail.Key,
                        context.Trigger.Key);
                    context.SetExecutionDetails("已跳过：分布式锁不可用");
                    return;
                }

                // 分布式锁已被占用
                if (lockResult.Status == DistributedJobLockAcquireStatus.Contended)
                {
                    _logger.LogWarning(
                        "[{runInstanceId}] 分布式锁已被占用，跳过执行插件任务 Job={jobKey} Trigger={triggerKey} LockKey={lockKey}",
                        context.FireInstanceId,
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        lockResult.LockKey);
                    context.SetExecutionDetails("已跳过：分布式锁已被占用");
                    return;
                }

                // 分布式锁不可用
                if (lockResult.Status == DistributedJobLockAcquireStatus.Unavailable)
                {
                    _logger.LogWarning(
                        "[{runInstanceId}] 分布式锁不可用，跳过执行插件任务 Job={jobKey} Trigger={triggerKey} LockKey={lockKey}",
                        context.FireInstanceId,
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        lockResult.LockKey);
                    context.SetExecutionDetails("已跳过：分布式锁不可用");
                    return;
                }
            }

            // 从 JobDataMap 中获取插件执行描述（包含程序集、类型、方法、参数）
            var descriptorJson = jobDataMap.GetString(JobDataMapKeys.PluginExecutionDescriptor);
            if (string.IsNullOrWhiteSpace(descriptorJson))
            {
                ArgumentNullException.ThrowIfNull(jobInfo.JobPluginConfig);

                descriptorJson = JsonConvert.SerializeObject(new PluginExecutionDescriptor
                {
                    AssemblyPath = jobInfo.JobPluginConfig.AssemblyPath,
                    MethodName = jobInfo.JobPluginConfig.MethodName,
                    TypeFullName = jobInfo.JobPluginConfig.TypeFullName,
                    Version = jobInfo.JobPluginConfig.Version,
                    Parameters = PluginParameterJsonHelper.DeserializeExecutionParameters(jobInfo.JobPluginConfig.ParamsJson)
                });
            }

            var descriptor = JsonConvert.DeserializeObject<PluginExecutionDescriptor>(descriptorJson);
            ArgumentNullException.ThrowIfNull(descriptor, nameof(descriptor));

            var assembly = PluginExecutionRuntime.LoadPluginAssembly(descriptor.AssemblyPath, descriptor.Version);
            var pluginType = assembly.GetType(descriptor.TypeFullName, throwOnError: true)!;
            var method = PluginExecutionRuntime.ResolveMethod(pluginType, descriptor.MethodName, descriptor.Parameters, assembly);
            var parameters = PluginExecutionRuntime.BuildParameters(method, descriptor.Parameters, assembly);
            var instance = method.IsStatic ? null : Activator.CreateInstance(pluginType)
                ?? throw new InvalidOperationException($"无法创建插件类型 {pluginType.FullName} 的实例");
            var result = await PluginExecutionRuntime.InvokeAsync(method, instance, parameters).ConfigureAwait(false);

            context.Result = result;
            context.SetIsSuccess(true);
            context.SetExecutionDetails($"插件: [{descriptor.TypeFullName}.{descriptor.MethodName}]");
        }
        catch (JobExecutionException)
        {
            context.SetIsSuccess(false);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "执行插件任务失败[{runInstanceId}]", context.FireInstanceId);
            context.SetIsSuccess(false);
            throw new JobExecutionException("执行插件任务失败", ex);
        }
        finally
        {
            if (lockResult?.Status == DistributedJobLockAcquireStatus.Acquired
                && !string.IsNullOrWhiteSpace(lockResult.LockToken))
            {
                await _distributedJobLockService.ReleaseAsync(
                    lockResult.LockKey,
                    lockResult.LockToken,
                    context.CancellationToken);
            }
        }
    }
}
