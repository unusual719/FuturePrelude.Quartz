namespace FuturePrelude.Quartz.Listener;

/// <summary>
/// 全局作业监听器：负责记录执行日志并回写最近运行信息。
/// <para> 实现 <see cref="IJobListener" /> 接口，在任务执行的各个阶段进行拦截：
/// <list type="bullet">
/// <item> <see cref="JobToBeExecuted" /> - 任务执行前，记录开始日志 </item>
/// <item> <see cref="JobWasExecuted" /> - 任务执行后（成功/失败），更新结束状态和结果 </item>
/// <item> <see cref="JobExecutionVetoed" /> - 任务被否决时，记录取消状态 </item>
/// </list>
/// </para>
/// </summary>
public sealed class GlobalJobListener : IJobListener
{
    /// <summary> 执行来源：普通调度 </summary>
    private const string SchedulerSource = "Scheduler";

    /// <summary> 执行来源：重试执行 </summary>
    private const string RetrySource = "Retry";

    /// <summary> 执行来源：故障恢复 </summary>
    private const string RecoverySource = "Recovery";

    private readonly IFreeSql _freeSql;
    private readonly IDashboardNotifier _dashboardNotifier;
    private readonly ILogger<GlobalJobListener> _logger;

    /// <summary> 初始化全局作业监听器 </summary>
    /// <param name="freeSql"> </param>
    /// <param name="dashboardNotifier"> </param>
    /// <param name="logger"> </param>
    public GlobalJobListener(
        IFreeSql freeSql,
        IDashboardNotifier dashboardNotifier,
        ILogger<GlobalJobListener> logger)
    {
        _freeSql = freeSql;
        _dashboardNotifier = dashboardNotifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => nameof(GlobalJobListener);

    /// <summary>
    /// 解析取消令牌
    /// <para> 优先使用传入的 cancellationToken，若为 default 则使用 context 内置的 CancellationToken </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private static CancellationToken ResolveCancellationToken(
            IJobExecutionContext context,
            CancellationToken cancellationToken)
    {
        return cancellationToken != default ? cancellationToken : context.CancellationToken;
    }

    /// <summary>
    /// 解析 RunId（执行批次ID）
    /// <para> 优先使用 FireInstanceId，若为空则生成新的 GUID </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static string ResolveRunId(IJobExecutionContext context)
    {
        return string.IsNullOrWhiteSpace(context.FireInstanceId)
            ? Guid.NewGuid().ToString("N")
            : context.FireInstanceId;
    }

    /// <summary>
    /// 解析执行来源
    /// <para> 根据 context.Recovering 和 RefireCount 判断执行来源：普通调度/重试/故障恢复 </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static string ResolveExecutionSource(IJobExecutionContext context)
    {
        if (context.Recovering)
        {
            return RecoverySource;
        }

        if (context.RefireCount > 0)
        {
            return RetrySource;
        }

        return SchedulerSource;
    }

    /// <summary>
    /// 解析计划触发时间
    /// <para> 优先使用 ScheduledFireTimeUtc，若为空则使用 FireTimeUtc </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static DateTime? ResolveScheduledFireTimeUtc(IJobExecutionContext context)
    {
        return ChinaTimeZoneConverter.FromOffset(context.ScheduledFireTimeUtc ?? context.FireTimeUtc);
    }

    /// <summary>
    /// 解析要持久化到触发器表的上一次运行时间
    /// <para> <see cref="IJobListener.JobWasExecuted" /> 触发时，Quartz 还未把触发器内部状态推进到下一轮， 因此这里使用“本次计划触发时间”作为业务表的最近运行时间回写。 </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static DateTime? ResolvePersistedPrevFireTimeUtc(IJobExecutionContext context)
    {
        return ResolveScheduledFireTimeUtc(context);
    }

    /// <summary> 构建 JobKey 字符串（格式：Group:Name） </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static string BuildJobKey(IJobExecutionContext context)
        => $"{context.JobDetail.Key.Group}:{context.JobDetail.Key.Name}";

    /// <summary> 构建 TriggerKey 字符串（格式：Group:Name） </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static string BuildTriggerKey(IJobExecutionContext context)
        => $"{context.Trigger.Key.Group}:{context.Trigger.Key.Name}";

    /// <summary>
    /// 尝试获取任务开始执行的时间
    /// <para> 优先从 JobDataMap 中获取 ExecutionStartTime，若不存在则使用 FireTimeUtc </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <returns> </returns>
    private static DateTime? TryGetStartTimeUtc(IJobExecutionContext context)
    {
        var value = context.Get(JobDataMapKeys.ExecutionStartTime);
        if (value == null)
        {
            return ChinaTimeZoneConverter.FromOffset(context.FireTimeUtc);
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
        {
            return ChinaTimeZoneConverter.FromOffset(context.FireTimeUtc);
        }

        if (DateTime.TryParseExact(
            text,
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var legacyBeijingTime))
        {
            return legacyBeijingTime;
        }

        if (DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var dto))
        {
            return ChinaTimeZoneConverter.FromOffset(dto);
        }

        if (DateTime.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out var date))
        {
            return date;
        }

        return ChinaTimeZoneConverter.FromOffset(context.FireTimeUtc);
    }

    /// <summary>
    /// 计算任务执行耗时（毫秒）
    /// <para> 优先从 JobDataMap 获取 ExecutionDuration，若获取失败则根据开始和结束时间计算 </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <param name="startTimeUtc"> </param>
    /// <param name="endTimeUtc"> </param>
    /// <returns> </returns>
    private static double? ResolveDurationMs(
            IJobExecutionContext context,
            DateTime startTimeUtc,
            DateTime endTimeUtc)
    {
        var value = context.Get(JobDataMapKeys.ExecutionDuration);
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var durationMs))
        {
            return durationMs;
        }

        var computed = (endTimeUtc - startTimeUtc).TotalMilliseconds;
        return computed < 0 ? 0 : computed;
    }

    /// <summary>
    /// 根据执行上下文和异常信息解析运行状态
    /// <para> 判断优先级：vetoed &gt; cancelled &gt; timeout &gt; success/failed </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <param name="jobException"> </param>
    /// <param name="isVetoed"> </param>
    /// <param name="executionDetails"> </param>
    /// <returns> </returns>
    private static RunStatus ResolveRunStatus(
            IJobExecutionContext context,
            JobExecutionException? jobException,
            bool isVetoed,
            string? executionDetails)
    {
        if (isVetoed)
        {
            return RunStatus.Cancelled;
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return RunStatus.Cancelled;
        }

        if (IsSkippedExecution(executionDetails))
        {
            return RunStatus.Cancelled;
        }

        var exception = jobException?.InnerException ?? jobException;
        if (exception is TimeoutException || exception?.GetType().Name.Contains("Timeout", StringComparison.OrdinalIgnoreCase) == true)
        {
            return RunStatus.Timeout;
        }

        var isSuccess = context.GetIsSuccess();
        if (isSuccess == true)
        {
            return RunStatus.Success;
        }

        if (jobException != null || isSuccess == false)
        {
            return RunStatus.Failed;
        }

        return RunStatus.Success;
    }

    /// <summary>
    /// 解析原因代码（ReasonCode）
    /// <para> 用于标识特殊执行结果：VETOED/CANCELLED/LOCK_CONTENDED/LOCK_UNAVAILABLE/SKIPPED/TIMEOUT/JOB_EXECUTION_EXCEPTION/FAILED </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <param name="jobException"> </param>
    /// <param name="isVetoed"> </param>
    /// <param name="executionDetails"> </param>
    /// <returns> </returns>
    private static string? ResolveReasonCode(
            IJobExecutionContext context,
            JobExecutionException? jobException,
            bool isVetoed,
            string? executionDetails)
    {
        if (isVetoed)
        {
            return "VETOED";
        }

        if (context.CancellationToken.IsCancellationRequested)
        {
            return "CANCELLED";
        }

        if (!string.IsNullOrWhiteSpace(executionDetails))
        {
            if (executionDetails.Contains("分布式锁已被占用", StringComparison.OrdinalIgnoreCase))
            {
                return "LOCK_CONTENDED";
            }

            if (executionDetails.Contains("分布式锁不可用", StringComparison.OrdinalIgnoreCase))
            {
                return "LOCK_UNAVAILABLE";
            }

            if (executionDetails.Contains("已跳过", StringComparison.OrdinalIgnoreCase))
            {
                return "SKIPPED";
            }
        }

        var exception = jobException?.InnerException ?? jobException;
        if (exception is TimeoutException || exception?.GetType().Name.Contains("Timeout", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "TIMEOUT";
        }

        if (jobException != null)
        {
            return "JOB_EXECUTION_EXCEPTION";
        }

        if (context.GetIsSuccess() == false)
        {
            return "FAILED";
        }

        return null;
    }

    /// <summary>
    /// 解析原因消息（ReasonMessage）
    /// <para> vetoed 时返回固定消息，否则返回 executionDetails 或异常消息 </para>
    /// </summary>
    /// <param name="jobException"> </param>
    /// <param name="isVetoed"> </param>
    /// <param name="executionDetails"> </param>
    /// <returns> </returns>
    private static string? ResolveReasonMessage(
            JobExecutionException? jobException,
            bool isVetoed,
            string? executionDetails)
    {
        if (isVetoed)
        {
            return "任务执行被 veto";
        }

        if (!string.IsNullOrWhiteSpace(executionDetails))
        {
            return executionDetails;
        }

        return jobException?.InnerException?.Message ?? jobException?.Message;
    }

    /// <summary>
    /// 判断执行是否被跳过
    /// <para> 当 executionDetails 包含"已跳过"时返回 true </para>
    /// </summary>
    /// <param name="executionDetails"> </param>
    /// <returns> </returns>
    private static bool IsSkippedExecution(string? executionDetails)
    {
        return !string.IsNullOrWhiteSpace(executionDetails)
            && executionDetails.Contains("已跳过", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary> 构建返回值 </summary>
    /// <param name="context"> </param>
    /// <param name="jobException"> </param>
    /// <returns> </returns>
    private static string? BuildReturnValue(
            IJobExecutionContext context,
            JobExecutionException? jobException)
    {
        var value = context.Get(JobDataMapKeys.ExecutionResponseContent);
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        return text;
    }

    /// <summary>
    /// 构建执行上下文 JSON 字符串
    /// <para> 用于存储到 SysJobExecutionLog.ExecutionContextJson 字段，便于事后分析和排障 </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <param name="result"> </param>
    /// <param name="reasonCode"> </param>
    /// <param name="executionDetails"> </param>
    /// <returns> </returns>
    private static string BuildExecutionContextJson(
            IJobExecutionContext context,
            RunStatus result,
            string? reasonCode,
            string? executionDetails)
    {
        return JsonConvert.SerializeObject(new {
            JobKey = BuildJobKey(context),
            TriggerKey = BuildTriggerKey(context),
            FireInstanceId = context.FireInstanceId,
            ScheduledFireTimeUtc = ResolveScheduledFireTimeUtc(context),
            FireTimeUtc = ChinaTimeZoneConverter.FromOffset(context.FireTimeUtc),
            NextFireTimeUtc = context.NextFireTimeUtc.HasValue
                ? (DateTime?)ChinaTimeZoneConverter.FromOffset(context.NextFireTimeUtc.Value)
                : null,
            PreviousFireTimeUtc = context.PreviousFireTimeUtc.HasValue
                ? (DateTime?)ChinaTimeZoneConverter.FromOffset(context.PreviousFireTimeUtc.Value)
                : null,
            RefireCount = context.RefireCount,
            IsRecovering = context.Recovering,
            ReturnCode = context.GetReturnCode(),
            IsSuccess = context.GetIsSuccess(),
            Result = result.ToString(),
            ReasonCode = reasonCode,
            ExecutionDetails = executionDetails
        });
    }

    /// <summary>
    /// 任务执行结束时的统一处理逻辑
    /// <para> 此方法被 <see cref="JobWasExecuted" /> 和 <see cref="JobExecutionVetoed" /> 共同调用，统一处理日志写入和状态回写 </para>
    /// </summary>
    /// <param name="context"> </param>
    /// <param name="jobException"> </param>
    /// <param name="isVetoed"> </param>
    /// <param name="cancellationToken"> </param>
    private async Task FinalizeExecutionAsync(
            IJobExecutionContext context,
            JobExecutionException? jobException,
            bool isVetoed,
            CancellationToken cancellationToken)
    {
        var token = ResolveCancellationToken(context, cancellationToken);
        var resolved = await ResolveExecutionTargetAsync(context, token);
        var runId = ResolveRunId(context);
        var endTimeUtc = ChinaTimeZoneConverter.Now();
        var existing = await _freeSql.Select<SysJobExecutionLog>()
            .Where(a => a.RunId == runId)
            .FirstAsync(token);

        var startTimeUtc = existing?.StartTimeUtc ?? TryGetStartTimeUtc(context) ?? endTimeUtc;
        var durationMs = ResolveDurationMs(context, startTimeUtc, endTimeUtc);
        var executionDetails = context.GetExecutionDetails();
        var result = ResolveRunStatus(context, jobException, isVetoed, executionDetails);
        var reasonCode = ResolveReasonCode(context, jobException, isVetoed, executionDetails);
        var reasonMessage = ResolveReasonMessage(jobException, isVetoed, executionDetails);
        var returnValue = BuildReturnValue(context, jobException);
        var exception = jobException?.InnerException ?? jobException;

        if (existing == null)
        {
            // 执行日志记录
            existing = new SysJobExecutionLog
            {
                JobId = resolved.Job?.Id,
                TriggerId = resolved.Trigger?.Id,
                JobType = resolved.Job?.JobType,
                JobKey = BuildJobKey(context),
                TriggerKey = BuildTriggerKey(context),
                JobNameSnapshot = resolved.Job?.JobName ?? context.JobDetail.Key.Name,
                TriggerNameSnapshot = resolved.Trigger?.TriggerName ?? context.Trigger.Key.Name,
                RunId = runId,
                RootRunId = runId,
                AttemptNo = Math.Max(1, context.RefireCount + 1),
                ExecutionSource = ResolveExecutionSource(context),
                ExecutionSummary = context.GetExecutionDetails(),
                FireInstanceId = context.FireInstanceId,
                ScheduledFireTimeUtc = ResolveScheduledFireTimeUtc(context),
                ReturnValue = returnValue,
                StartTimeUtc = startTimeUtc,
                CreateBy = resolved.Job?.CreateBy,
                CreateTime = startTimeUtc
            };

            await _freeSql.Insert(existing).ExecuteAffrowsAsync(token);
        }

        var executionContextJson = BuildExecutionContextJson(
            context,
            result,
            reasonCode,
            executionDetails);

        await _freeSql.Update<SysJobExecutionLog>()
            .Where(a => a.RunId == runId)
            .Set(a => a.EndTimeUtc, endTimeUtc)
            .Set(a => a.DurationMs, durationMs)
            .Set(a => a.Result, result.ToString())
            .Set(a => a.ExecutorNode, Environment.MachineName)
            .Set(a => a.ReasonCode, reasonCode)
            .Set(a => a.ReasonMessage, reasonMessage)
            .Set(a => a.ExceptionType, exception?.GetType().FullName)
            .Set(a => a.Exception, exception?.Message)
            .Set(a => a.StackTrace, exception?.ToString())
            .Set(a => a.ReturnValue, returnValue)
            .Set(a => a.ExecutionContextJson, executionContextJson)
            .Set(a => a.ExecutionSummary, context.GetExecutionDetails())
            .ExecuteAffrowsAsync(token);

        if (resolved.Trigger != null)
        {
            // 回写触发器表的最近触发时间和下一次触发时间
            await _freeSql.Update<SysJobTrigger>()
                .Where(a => a.Id == resolved.Trigger.Id)
                .Set(a => a.PrevFireTimeUtc, ResolvePersistedPrevFireTimeUtc(context))
                .Set(a => a.NextFireTimeUtc, context.NextFireTimeUtc.HasValue
                    ? ChinaTimeZoneConverter.FromOffset(context.NextFireTimeUtc.Value)
                    : null)
                .Set(a => a.UpdateTime, endTimeUtc)
                .ExecuteAffrowsAsync(token);
        }

        if (resolved.Job != null)
        {
            // 回写任务表的最近运行状态
            await _freeSql.Update<SysJobDetail>()
                .Where(a => a.Id == resolved.Job.Id)
                .Set(a => a.LastRunStatus, result)
                .Set(a => a.LastRunTime, endTimeUtc)
                .Set(a => a.LastRunDurationMs, durationMs.HasValue
                    ? (int?)Math.Round(durationMs.Value, MidpointRounding.AwayFromZero)
                    : null)
                .Set(a => a.UpdateTime, endTimeUtc)
                .ExecuteAffrowsAsync(token);
        }

        await _dashboardNotifier.NotifyOverviewChangedAsync(token);
    }

    /// <summary> 从数据库中解析任务和触发器元数据 </summary>
    /// <param name="context"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task<(SysJobDetail? Job, SysJobTrigger? Trigger)> ResolveExecutionTargetAsync(
            IJobExecutionContext context,
            CancellationToken cancellationToken)
    {
        var jobName = context.JobDetail.Key.Name?.Trim();
        var jobGroupName = context.JobDetail.Key.Group?.Trim();
        if (string.IsNullOrWhiteSpace(jobName) || string.IsNullOrWhiteSpace(jobGroupName))
        {
            return (null, null);
        }

        var group = await _freeSql.Select<SysJobGroup>()
            .Where(a => !a.IsDeleted && (a.Key == jobGroupName || a.Name == jobGroupName))
            .OrderBy(a => a.Id)
            .FirstAsync(cancellationToken);
        if (group == null)
        {
            return (null, null);
        }

        var jobKeyCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{group.Key}:{jobName}",
            $"{jobGroupName}:{jobName}"
        };
        if (!string.IsNullOrWhiteSpace(group.Name))
        {
            jobKeyCandidates.Add($"{group.Name}:{jobName}");
        }

        var job = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.GroupId == group.Id && !a.IsDeleted)
            .Where(a => a.JobName == jobName || jobKeyCandidates.Contains(a.JobKey))
            .OrderBy(a => a.Id)
            .FirstAsync(cancellationToken);
        if (job == null)
        {
            return (null, null);
        }

        var triggerName = context.Trigger.Key.Name;
        var triggerGroup = context.Trigger.Key.Group;
        var triggerKey = $"{triggerGroup}:{triggerName}";

        var trigger = await _freeSql.Select<SysJobTrigger>()
            .Where(a => a.JobId == job.Id && !a.IsDeleted)
            .Where(a =>
                (a.TriggerName == triggerName && a.TriggerGroup == triggerGroup) ||
                a.TriggerKey == triggerKey)
            .OrderBy(a => a.Id)
            .FirstAsync(cancellationToken);

        return (job, trigger);
    }

    /// <summary> 任务即将执行前触发 </summary>
    /// <param name="context"> </param>
    /// <param name="cancellationToken"> </param>
    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var token = ResolveCancellationToken(context, cancellationToken);
        var resolved = await ResolveExecutionTargetAsync(context, token);
        var runId = ResolveRunId(context);
        var startTimeUtc = ChinaTimeZoneConverter.Now();

        context.Put(
            JobDataMapKeys.ExecutionStartTime,
            ChinaTimeZoneConverter.ToChinaOffset(startTimeUtc).ToString("O", CultureInfo.InvariantCulture));

        if (resolved.Job == null)
        {
            _logger.LogWarning("监听器未找到作业元数据，跳过写入开始日志。Job={JobKey}", context.JobDetail.Key);
            return;
        }

        var exists = await _freeSql.Select<SysJobExecutionLog>()
            .Where(a => a.RunId == runId)
            .AnyAsync(token);
        if (exists)
        {
            return;
        }

        var log = new SysJobExecutionLog
        {
            JobId = resolved.Job.Id,
            TriggerId = resolved.Trigger?.Id,
            JobType = resolved.Job.JobType,
            JobKey = BuildJobKey(context),
            TriggerKey = BuildTriggerKey(context),
            JobNameSnapshot = resolved.Job.JobName,
            TriggerNameSnapshot = resolved.Trigger?.TriggerName ?? context.Trigger.Key.Name,
            RunId = runId,
            RootRunId = runId,
            AttemptNo = Math.Max(1, context.RefireCount + 1),
            ExecutionSource = ResolveExecutionSource(context),
            FireInstanceId = context.FireInstanceId,
            ScheduledFireTimeUtc = ResolveScheduledFireTimeUtc(context),
            StartTimeUtc = startTimeUtc,
            Result = RunStatus.Pending.ToString(),
            CreateBy = resolved.Job.CreateBy,
            CreateTime = startTimeUtc
        };

        await _freeSql.Insert(log).ExecuteAffrowsAsync(token);
    }

    /// <summary> 任务被否决时触发 </summary>
    /// <param name="context"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        await FinalizeExecutionAsync(
            context,
            jobException: null,
            isVetoed: true,
            cancellationToken: cancellationToken);
    }

    /// <summary> 任务执行完成后触发（无论成功或失败） </summary>
    /// <param name="context"> </param>
    /// <param name="jobException"> </param>
    /// <param name="cancellationToken"> </param>
    public async Task JobWasExecuted(
        IJobExecutionContext context,
        JobExecutionException? jobException,
        CancellationToken cancellationToken = default)
    {
        await FinalizeExecutionAsync(
            context,
            jobException,
            isVetoed: false,
            cancellationToken: cancellationToken);
    }
}