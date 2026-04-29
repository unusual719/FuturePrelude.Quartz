namespace FuturePrelude.Quartz;

/// <summary> JobStorage 服务定义 </summary>
public interface IJobStorageService
{
    /// <summary> 根据作业名称和分组获取单个定时作业信息 </summary>
    /// <param name="jobName"> </param>
    /// <param name="jobGroupName"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    Task<SysJobDetail?> GetJobAsync(string jobName, string jobGroupName, CancellationToken cancellationToken = default);
}
