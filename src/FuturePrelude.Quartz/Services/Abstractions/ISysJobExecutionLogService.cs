namespace FuturePrelude.Quartz.Services;

/// <summary> 任务执行记录服务接口 </summary>
public interface ISysJobExecutionLogService
{
    /// <summary> 分页获取任务执行记录 </summary>
    /// <param name="query"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<PageResponse<SysJobExecutionLogOutput>> GetPageListAsync(
        SysJobExecutionLogQueryInput query,
        CancellationToken cancellationToken = default);

    /// <summary> 删除近30天前的日志数据 </summary>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<int> DeleteLogsOlderThan30DaysAsync(CancellationToken cancellationToken = default);
}