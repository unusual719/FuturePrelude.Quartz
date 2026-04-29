namespace FuturePrelude.Quartz;

/// <summary> 将实体映射为 Quartz 所需的 Job 与 Trigger </summary>
internal static class SchedulerEntityMapper
{
    /// <summary> 构建 CronTrigger </summary>
    /// <param name="builder"> </param>
    /// <param name="trigger"> </param>
    /// <returns> </returns>
    /// <exception cref="ArgumentException"> </exception>
    private static ITrigger BuildCronTrigger(TriggerBuilder builder, SysJobTrigger trigger)
    {
        var cronConfig = TriggerConfigJsonHelper.DeserializeCron(trigger.TypeConfigJson);
        ArgumentNullException.ThrowIfNull(cronConfig.CronExpression);
        if (!CronExpression.IsValidExpression(cronConfig.CronExpression))
        {
            throw new ArgumentException($"无效的 Cron 表达式：{cronConfig.CronExpression}");
        }

        var timeZone = TimeZoneDefaults.ResolveTimeZone(trigger.TimeZoneId) ?? TimeZoneInfo.Utc;
        var cron = CronScheduleBuilder
            .CronSchedule(cronConfig.CronExpression)
            .InTimeZone(timeZone);

        cron = trigger.MisfireStrategy switch
        {
            MisfireAction.DoNothing => cron.WithMisfireHandlingInstructionDoNothing(),
            MisfireAction.FireOnceNow => cron.WithMisfireHandlingInstructionFireAndProceed(),
            MisfireAction.IgnoreMisfirePolicy => cron.WithMisfireHandlingInstructionIgnoreMisfires(),
            _ => cron.WithMisfireHandlingInstructionFireAndProceed()
        };

        builder.WithSchedule(cron);
        return builder.Build();
    }

    /// <summary> 解析 JobKey，支持 "Group:Name" 格式 </summary>
    /// <param name="job"> </param>
    /// <returns> </returns>
    /// <exception cref="NotImplementedException"> </exception>
    internal static (string Group, string Name) ParseJobKey(SysJobDetail job)
    {
        if (SchedulerIdentityHelper.TryParseCompositeKey(job.JobKey, out var group, out var name))
        {
            return (group, name);
        }

        throw new NotImplementedException("SchedulerEntityMapper.ParseJobKey -> Group:Name 非法");
    }

    /// <summary> 解析 TriggerKey，支持 "Group:Name" 格式 </summary>
    /// <param name="trigger"> </param>
    /// <returns> </returns>
    /// <exception cref="NotImplementedException"> </exception>
    internal static (string Group, string Name) ParseTriggerKey(SysJobTrigger trigger)
    {
        if (SchedulerIdentityHelper.TryParseCompositeKey(trigger.TriggerKey, out var group, out var name))
        {
            return (group, name);
        }

        throw new NotImplementedException("SchedulerEntityMapper.ParseTriggerKey -> Group:Name 非法");
    }

    /// <summary> 构建 HttpJob 的 JobDetail </summary>
    /// <param name="job"> </param>
    /// <param name="httpConfig"> </param>
    /// <returns> </returns>
    internal static IJobDetail BuildHttpJobDetail(SysJobDetail job, SysJobHttpConfig httpConfig)
    {
        var (group, name) = ParseJobKey(job);
        var dataMap = BuildDataMap(httpConfig);
        return JobBuilder.Create<HttpApiJob>()
            .WithIdentity(new JobKey(name, group))
            .WithDescription(job.Description)
            .UsingJobData(dataMap)
            .RequestRecovery(true) // Scheduler 异常宕机，恢复后重新执行这个 Job
            .StoreDurably(true) // 没有 Trigger，这个 Job 也会被保存
            .Build();

        // 将插件元数据及参数序列化为 JobDataMap
        static JobDataMap BuildDataMap(SysJobHttpConfig httpConfig)
        {
            var map = new JobDataMap
            {
                { JobDataMapKeys.HttpMethod, httpConfig.RequestMethod },
                { JobDataMapKeys.HttpUrl, httpConfig.RequestUrl },
                { JobDataMapKeys.HttpRequestBody, httpConfig.RequestBody },
                { JobDataMapKeys.HttpTimeout, $"{httpConfig.TimeoutSeconds}" },
                { JobDataMapKeys.HttpHeaders, httpConfig.RequestHeaders }
            };

            return map;
        }
    }

    /// <summary> 构建插件 PluginJob 的 JobDetail </summary>
    /// <param name="job"> </param>
    /// <param name="plugin"> </param>
    /// <returns> </returns>
    internal static IJobDetail BuildPluginJobDetail(SysJobDetail job, SysJobPlugin plugin)
    {
        // JobKey 拆分组名和任务名，默认组使用 InternalConstants.DEFAULT_GROUP_NAME
        var (group, name) = ParseJobKey(job);
        var dataMap = BuildDataMap(plugin);

        return JobBuilder.Create<AssemblyPluginJob>()
            .WithIdentity(name, group)
            .WithDescription(job.Description)
            .UsingJobData(dataMap)
            .RequestRecovery(true) // Scheduler 异常宕机，恢复后重新执行这个 Job
            .StoreDurably(true) // 没有 Trigger，这个 Job 也会被保存
            .Build();

        // 将插件元数据及参数序列化为 JobDataMap
        static JobDataMap BuildDataMap(SysJobPlugin plugin)
        {
            var descriptor = new PluginExecutionDescriptor
            {
                AssemblyPath = plugin.AssemblyPath,
                TypeFullName = plugin.TypeFullName,
                MethodName = plugin.MethodName,
                PackageName = plugin.PackageName,
                Version = plugin.Version,
                Parameters = PluginParameterJsonHelper.DeserializeExecutionParameters(plugin.ParamsJson)
            };

            var map = new JobDataMap
            {
                { JobDataMapKeys.PluginExecutionDescriptor, JsonConvert.SerializeObject(descriptor) }
            };

            return map;
        }
    }

    /// <summary> 构建 Quartz Trigger（Cron 支持） </summary>
    /// <param name="trigger"> </param>
    /// <param name="job"> </param>
    /// <returns> </returns>
    /// <exception cref="NotSupportedException"> </exception>
    internal static ITrigger BuildTrigger(SysJobTrigger trigger, SysJobDetail? job = null)
    {
        var (triggerGroup, triggerName) = ParseTriggerKey(trigger);
        var builder = TriggerBuilder.Create()
            .WithIdentity(new TriggerKey(triggerName, triggerGroup))
            .WithDescription(trigger.Description ?? "无描述")
            .WithPriority(trigger.Priority > 0 ? trigger.Priority : 5); // 配置优先级

        // StartTime 默认当前 UTC；EndTime 可空
        DateTime start;
        if (!trigger.StartTimeUtc.HasValue)
        {
            builder.StartNow();
            start = DateTime.UtcNow;
        }
        else
        {
            start = ChinaTimeZoneConverter.ToUtc(trigger.StartTimeUtc.Value);
            builder.StartAt(new DateTimeOffset(start, TimeSpan.Zero));
        }

        // EndTime 结束时间
        if (trigger.EndTimeUtc.HasValue)
        {
            // 统一换算到 UTC 后再比较，避免“字段名保留 Utc、值语义为北京时间”时出现混合比较。
            var end = ChinaTimeZoneConverter.ToUtc(trigger.EndTimeUtc.Value);

            if (end <= start)
            {
                throw new ArgumentException("EndTime 必须大于 StartTime");
            }

            builder = builder.EndAt(new DateTimeOffset(end, TimeSpan.Zero));
        }

        // 关联 Job
        var ownerJob = job ?? trigger.JobDetail;
        if (ownerJob == null)
            throw new InvalidOperationException("Trigger 必须绑定 Job");
        else
        {
            var (jobGroup, jobName) = ParseJobKey(ownerJob);
            builder = builder.ForJob(jobName, jobGroup);
        }

        return trigger.TriggerType switch
        {
            TriggerType.Cron => BuildCronTrigger(builder, trigger),
            _ => throw new NotSupportedException(
                $"不支持的触发器类型: {trigger.TriggerType.GetDescription()}，当前仅支持 Cron")
        };
    }
}
