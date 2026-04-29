namespace FuturePrelude.Quartz.Services;

/// <summary> 仪表盘服务实现，提供任务执行统计、趋势、健康状态等数据 </summary>
public sealed class DashboardService(IFreeSql freeSql, ISchedulerService schedulerService) : IDashboardService
{
    /// <summary> Scheduler 状态查询的最大并发度 </summary>
    private const int SchedulerStateLookupConcurrency = 8;

    /// <summary> 失败状态结果数组（用于统计失败数） </summary>
    private static readonly string[] FailureResults =
    [
        nameof(RunStatus.Failed),
        nameof(RunStatus.Timeout),
        nameof(RunStatus.Cancelled)
    ];

    /// <summary> 终态结果数组（已结束的执行状态，用于计算成功率） </summary>
    private static readonly string[] TerminalResults =
    [
        nameof(RunStatus.Success),
        nameof(RunStatus.Failed),
        nameof(RunStatus.Timeout),
        nameof(RunStatus.Cancelled)
    ];

    private readonly IFreeSql _freeSql = freeSql;
    private readonly ISchedulerService _schedulerService = schedulerService;

    /// <summary> 将执行日志映射为最近执行记录的输出模型 </summary>
    /// <param name="log"> </param>
    /// <param name="jobLookup"> </param>
    /// <param name="groupLookup"> </param>
    /// <returns> </returns>
    private static DashboardRecentExecutionOutput MapRecentExecution(
        SysJobExecutionLog log,
        IReadOnlyDictionary<long, SysJobDetail> jobLookup,
        IReadOnlyDictionary<long, string> groupLookup)
    {
        jobLookup.TryGetValue(log.JobId ?? 0, out var job);

        var taskGroup = job != null && groupLookup.TryGetValue(job.GroupId, out var groupName)
            ? groupName
            : "未分组";

        return new DashboardRecentExecutionOutput
        {
            Id = log.Id,
            TaskName = log.JobNameSnapshot ?? job?.JobName ?? "-",
            TaskGroup = taskGroup,
            ExecutedAt = log.EndTimeUtc ?? log.StartTimeUtc ?? log.CreateTime,
            Status = log.Result ?? nameof(RunStatus.Pending),
            StatusText = ResolveStatusText(log.Result),
            DurationMs = log.DurationMs,
            Message = log.ExecutionSummary ?? log.ReasonMessage ?? log.ReturnValue ?? log.Exception ?? "-"
        };
    }

    /// <summary> 根据执行结果代码解析中文状态文本 </summary>
    /// <param name="result"> </param>
    /// <returns> </returns>
    private static string ResolveStatusText(string? result)
    {
        return result switch
        {
            nameof(RunStatus.Success) => "成功",
            nameof(RunStatus.Failed) => "失败",
            nameof(RunStatus.Timeout) => "超时",
            nameof(RunStatus.Cancelled) => "取消",
            nameof(RunStatus.Retrying) => "重试中",
            nameof(RunStatus.Pending) => "运行中",
            _ => "未知"
        };
    }

    /// <summary> 构建过去24小时的执行趋势数据 </summary>
    /// <param name="trendStart"> </param>
    /// <param name="trendLogs"> </param>
    /// <returns> </returns>
    private List<DashboardTrendPointOutput> BuildTrend(
        DateTime trendStart,
        IReadOnlyList<SysJobExecutionLog> trendLogs)
    {
        var bucketLookup = trendLogs
            .GroupBy(a => new DateTime(a.CreateTime.Year, a.CreateTime.Month, a.CreateTime.Day, a.CreateTime.Hour, 0, 0))
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    SuccessCount = group.Count(a => a.Result == nameof(RunStatus.Success)),
                    FailureCount = group.Count(a => FailureResults.Contains(a.Result))
                });

        return Enumerable.Range(0, 24)
            .Select(index =>
            {
                var bucketTime = trendStart.AddHours(index);
                bucketLookup.TryGetValue(bucketTime, out var bucket);

                return new DashboardTrendPointOutput
                {
                    BucketTime = bucketTime,
                    SuccessCount = bucket?.SuccessCount ?? 0,
                    FailureCount = bucket?.FailureCount ?? 0
                };
            }).ToList();
    }

    /// <summary> 批量解析任务当前运行状态，避免逐个串行等待 Quartz 响应 </summary>
    /// <param name="jobs"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task<JobRuntimeState[]> ResolveJobStatesAsync(
        IReadOnlyList<SysJobDetail> jobs,
        CancellationToken cancellationToken)
    {
        if (jobs.Count == 0)
        {
            return [];
        }

        using var semaphore = new SemaphoreSlim(Math.Min(SchedulerStateLookupConcurrency, jobs.Count));
        var tasks = jobs.Select(async job =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var (group, name) = SchedulerEntityMapper.ParseJobKey(job);
                return await _schedulerService.GetJobStateAsync(group, name, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary> 构建仪表盘概览快照 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    private async Task<DashboardOverviewOutput> BuildOverviewAsync(CancellationToken cancellationToken)
    {
        // 获取中国时区当前时间
        var now = ChinaTimeZoneConverter.Now();

        // 今日起始时间（00:00:00）
        var todayStart = now.Date;

        // 当前小时整点时间（如 14:00:00）
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

        // 趋势图起始时间（当前小时往前推23小时，共24个数据点）
        var trendStart = currentHour.AddHours(-23);

        // 查询所有未删除的任务
        var jobs = await _freeSql.Select<SysJobDetail>()
            .Where(a => !a.IsDeleted)
            .ToListAsync(cancellationToken);

        // 查询趋势区间的执行日志（过去24小时，用于趋势图和今日统计）
        var trendLogs = await _freeSql.Select<SysJobExecutionLog>()
            .Where(a => a.CreateTime >= trendStart && a.CreateTime <= now)
            .ToListAsync(cancellationToken);

        // 查询最近8条执行记录（按时间倒序，用于最近执行列表）
        var recentLogs = await _freeSql.Select<SysJobExecutionLog>()
            .OrderByDescending(a => a.CreateTime)
            .Limit(8)
            .ToListAsync(cancellationToken);

        // 今日日志是趋势日志的子集，无需重复访问数据库
        var todayLogs = trendLogs
            .Where(a => a.CreateTime >= todayStart)
            .ToList();

        // 任务ID到任务实体的字典（用于快速查找任务信息）
        var jobLookup = jobs.ToDictionary(a => a.Id);

        // 仅查询最近执行记录需要展示的任务组，避免无用全表扫描
        var recentGroupIds = recentLogs
            .Select(a => a.JobId)
            .Where(a => a.HasValue)
            .Select(a => a!.Value)
            .Where(jobLookup.ContainsKey)
            .Select(a => jobLookup[a].GroupId)
            .Distinct()
            .ToArray();

        var groupLookup = recentGroupIds.Length == 0
            ? new Dictionary<long, string>()
            : (await _freeSql.Select<SysJobGroup>()
                .Where(a => recentGroupIds.Contains(a.Id) && !a.IsDeleted)
                .ToListAsync(cancellationToken))
                .ToDictionary(a => a.Id, a => a.Name);

        // 今日已完成的任务（有结束时间且有耗时记录）
        var completedTodayLogs = todayLogs
            .Where(a => a.EndTimeUtc != null && a.DurationMs != null)
            .ToList();

        // 今日处于终态的任务（成功/失败/超时/，用于计算成功率）
        var terminalTodayLogs = todayLogs
            .Where(a => TerminalResults.Contains(a.Result))
            .ToList();

        var jobStates = await ResolveJobStatesAsync(jobs, cancellationToken);

        // 已运行的任务数
        var runningTasks = jobStates.Count(a => a.IsRunning);
        // 已停止/未启用的任务数
        var stoppedTasks = jobStates.Count(a => a.IsPaused);
        // 其余任务 = （总任务 - 运行中任务 - 已停止任务）
        var idleOrAbnormalTasks = jobs.Count - runningTasks - stoppedTasks;

        return new DashboardOverviewOutput
        {
            // 数据最后更新时间
            LastUpdatedAt = now,

            // 统计摘要
            Summary = new DashboardSummaryOutput
            {
                // 任务总数
                TotalTasks = jobs.Count,

                // 当前运行中的任务数
                RunningTasks = runningTasks,

                // 当前处于空闲状态的任务数
                IdleTaskCount = idleOrAbnormalTasks,

                // 今日失败任务数（包含 Failed、Timeout、Cancelled 状态）
                FailedToday = todayLogs.Count(a => FailureResults.Contains(a.Result)),

                // 今日平均响应时间（毫秒），如果没有已完成任务则返回0
                AverageResponseMs = completedTodayLogs.Count == 0
                    ? 0
                    : (int)Math.Round(completedTodayLogs.Average(a => a.DurationMs!.Value), MidpointRounding.AwayFromZero),

                // 今日任务成功率（百分比），如果没有终态任务则返回0 成功率 = 成功任务数 / 终态任务总数 * 100
                SuccessRate = terminalTodayLogs.Count == 0
                    ? 0m
                    : Math.Round(
                        terminalTodayLogs.Count(a => a.Result == nameof(RunStatus.Success)) * 100m / terminalTodayLogs.Count,
                        2,
                        MidpointRounding.AwayFromZero)
            },

            // 执行趋势数据（过去24小时每小时的成功/失败/运行中统计）
            Trend = BuildTrend(trendStart, trendLogs),

            // 健康状态分布（饼图/柱状图数据） 绿色：运行中任务 | 灰色：已停止任务 | 红色：其余任务
            HealthDistribution =
            [
                new DashboardHealthDistributionOutput { Label = "运行中", Count = runningTasks, Color = "#10b981" },
                new DashboardHealthDistributionOutput { Label = "已停止", Count = stoppedTasks, Color = "#94a3b8" },
                new DashboardHealthDistributionOutput { Label = "空闲中", Count = idleOrAbnormalTasks, Color = "#ef4444" }
            ],

            // TODO：告警列表（暂未实现）
            Alerts = [],

            // 最近执行记录
            RecentExecutions = recentLogs
                .Select(a => MapRecentExecution(a, jobLookup, groupLookup))
                .ToList()
        };
    }

    /// <summary> 获取仪表盘概览数据（包含统计摘要、趋势图、健康分布、最近执行记录等） </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<DashboardOverviewOutput> GetOverviewAsync(CancellationToken cancellationToken = default)
        => await DashboardOverviewCache.GetOrCreateAsync(BuildOverviewAsync, cancellationToken);
}
