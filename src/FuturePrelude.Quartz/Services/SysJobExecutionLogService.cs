using FreeSql;

namespace FuturePrelude.Quartz.Services;

/// <summary> 任务执行记录服务实现 </summary>
public class SysJobExecutionLogService(IFreeSql freeSql) : ISysJobExecutionLogService
{
    private readonly IFreeSql _freeSql = freeSql;

    /// <summary> 应用执行结果过滤条件 </summary>
    /// <param name="select"> </param>
    /// <param name="result"> </param>
    /// <returns> </returns>
    private static ISelect<SysJobExecutionLog> ApplyResultFilter(
        ISelect<SysJobExecutionLog> select,
        string result)
    {
        var normalized = result.Trim().ToLowerInvariant();

        return normalized switch
        {
            "running" or "运行中"
                => select.Where(a =>
                    a.StartTimeUtc != null &&
                    a.EndTimeUtc == null &&
                    a.Result == nameof(RunStatus.Pending)),
            "pending" or "等待执行"
                => select.Where(a => a.Result == nameof(RunStatus.Pending)),
            "success" or "成功"
                => select.Where(a => a.Result == nameof(RunStatus.Success)),
            "failed" or "失败"
                => select.Where(a => a.Result == nameof(RunStatus.Failed)),
            "timeout" or "超时"
                => select.Where(a => a.Result == nameof(RunStatus.Timeout)),
            "cancelled" or "canceled" or "取消"
                => select.Where(a => a.Result == nameof(RunStatus.Cancelled)),
            "retrying" or "重试中"
                => select.Where(a => a.Result == nameof(RunStatus.Retrying)),
            _ when Enum.TryParse<RunStatus>(result, true, out var status)
                => select.Where(a => a.Result == status.ToString()),
            _ => select
        };
    }

    /// <summary> 构造任务执行记录出参 </summary>
    /// <param name="entity"> </param>
    /// <returns> </returns>
    private static SysJobExecutionLogOutput BuildOutput(SysJobExecutionLog entity)
    {
        var resultText = ResolveResultText(entity);

        return new SysJobExecutionLogOutput
        {
            Id = entity.Id,
            JobId = entity.JobId,
            TriggerId = entity.TriggerId,
            JobType = entity.JobType,
            JobKey = entity.JobKey,
            TriggerKey = entity.TriggerKey,
            JobNameSnapshot = entity.JobNameSnapshot,
            TriggerNameSnapshot = entity.TriggerNameSnapshot,
            RunId = entity.RunId,
            RootRunId = entity.RootRunId,
            AttemptNo = entity.AttemptNo,
            ExecutionSource = entity.ExecutionSource,
            FireInstanceId = entity.FireInstanceId,
            ScheduledFireTimeUtc = entity.ScheduledFireTimeUtc,
            StartTimeUtc = entity.StartTimeUtc,
            EndTimeUtc = entity.EndTimeUtc,
            DisplayTimeUtc = entity.EndTimeUtc ?? entity.StartTimeUtc ?? entity.CreateTime,
            DurationMs = entity.DurationMs,
            Result = entity.Result,
            ResultText = resultText,
            IsRunning = IsRunning(entity),
            ExecutorNode = entity.ExecutorNode,
            ReasonCode = entity.ReasonCode,
            ReasonMessage = entity.ReasonMessage,
            ExceptionType = entity.ExceptionType,
            Exception = entity.Exception,
            StackTrace = entity.StackTrace,
            ReturnValue = entity.ReturnValue,
            DisplayMessage = ResolveDisplayMessage(entity, resultText),
            ExecutionContextJson = entity.ExecutionContextJson,
            ExecutionSummary = entity.ExecutionSummary,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime
        };
    }

    /// <summary> 判断任务是否正在执行 </summary>
    /// <param name="entity"> </param>
    /// <returns> </returns>
    private static bool IsRunning(SysJobExecutionLog entity)
        => entity.StartTimeUtc.HasValue &&
           !entity.EndTimeUtc.HasValue &&
           string.Equals(entity.Result, RunStatus.Pending.ToString(), StringComparison.OrdinalIgnoreCase);

    /// <summary> 解析执行结果文本 </summary>
    /// <param name="entity"> </param>
    /// <returns> </returns>
    private static string ResolveResultText(SysJobExecutionLog entity)
    {
        if (IsRunning(entity))
        {
            return "运行中";
        }

        if (!string.IsNullOrWhiteSpace(entity.Result) &&
            Enum.TryParse<RunStatus>(entity.Result, true, out var status))
        {
            return status.GetDescription();
        }

        return string.IsNullOrWhiteSpace(entity.Result) ? "-" : entity.Result;
    }

    /// <summary> 解析显示消息 </summary>
    /// <param name="entity"> </param>
    /// <param name="resultText"> </param>
    /// <returns> </returns>
    private static string ResolveDisplayMessage(SysJobExecutionLog entity, string resultText)
    {
        if (!string.IsNullOrWhiteSpace(entity.ReturnValue))
        {
            return entity.ReturnValue;
        }

        if (!string.IsNullOrWhiteSpace(entity.Exception))
        {
            return entity.Exception;
        }

        if (!string.IsNullOrWhiteSpace(entity.ReasonMessage))
        {
            return entity.ReasonMessage;
        }

        return resultText;
    }

    /// <summary> 分页获取任务执行记录 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<PageResponse<SysJobExecutionLogOutput>> GetPageListAsync(
        SysJobExecutionLogQueryInput query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.PageIndex <= 0)
        {
            query.PageIndex = 1;
        }

        if (query.PageSize <= 0)
        {
            query.PageSize = 10;
        }

        var select = _freeSql.Select<SysJobExecutionLog>();

        if (query.JobId.HasValue && query.JobId.Value > 0)
        {
            select = select.Where(a => a.JobId == query.JobId.Value);
        }

        if (query.TriggerId.HasValue && query.TriggerId.Value > 0)
        {
            select = select.Where(a => a.TriggerId == query.TriggerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TaskName))
        {
            var taskName = query.TaskName.Trim();
            select = select.Where(a => a.JobNameSnapshot.Contains(taskName));
        }

        if (!string.IsNullOrWhiteSpace(query.Result))
        {
            select = ApplyResultFilter(select, query.Result);
        }

        if (query.StartTime.HasValue)
        {
            select = select.Where(a => a.CreateTime >= query.StartTime.Value);
        }

        if (query.EndTime.HasValue)
        {
            select = select.Where(a => a.CreateTime <= query.EndTime.Value);
        }

        var totalCount = await select.CountAsync(cancellationToken);
        var entities = await select
            .OrderByDescending(a => a.CreateTime)
            .Page(query.PageIndex, query.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResponse<SysJobExecutionLogOutput>
        {
            Items = entities.Select(BuildOutput).ToList(),
            TotalCount = (int)totalCount,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize
        };
    }

    /// <summary> 删除近30天前的日志数据 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    public async Task<int> DeleteLogsOlderThan30DaysAsync(CancellationToken cancellationToken = default)
    {
        var cutoffTime = ChinaTimeZoneConverter.Now().AddDays(-30);
        var deletedRows = await _freeSql.Delete<SysJobExecutionLog>()
            .Where(a => a.CreateTime < cutoffTime)
            .ExecuteAffrowsAsync(cancellationToken);

        return deletedRows;
    }
}