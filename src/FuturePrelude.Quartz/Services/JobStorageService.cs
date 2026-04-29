namespace FuturePrelude.Quartz;

/// <summary> JobStorage 服务定义实现 </summary>
public class JobStorageService(IFreeSql freeSql) : IJobStorageService
{
    private readonly IFreeSql _freeSql = freeSql;

    /// <summary> 根据作业名称和分组获取单个定时作业信息 </summary>
    /// <param name="jobName"> </param>
    /// <param name="jobGroupName"> </param>
    /// <param name="cancellationToken"> </param>
    /// <returns> </returns>
    /// <exception cref="NotImplementedException"> </exception>
    public async Task<SysJobDetail?> GetJobAsync(string jobName, string jobGroupName, CancellationToken cancellationToken = default)
    {
        var normalizedJobName = jobName?.Trim();
        var normalizedJobGroupName = jobGroupName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedJobName) || string.IsNullOrWhiteSpace(normalizedJobGroupName))
        {
            return null;
        }

        var group = await _freeSql.Select<SysJobGroup>()
            .Where(a => !a.IsDeleted && (a.Key == normalizedJobGroupName || a.Name == normalizedJobGroupName))
            .OrderBy(a => a.Id)
            .FirstAsync(cancellationToken);

        if (group == null)
        {
            return null;
        }

        var jobKeyCandidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{group.Key}:{normalizedJobName}",
            $"{normalizedJobGroupName}:{normalizedJobName}"
        };

        if (!string.IsNullOrWhiteSpace(group.Name))
        {
            jobKeyCandidates.Add($"{group.Name}:{normalizedJobName}");
        }

        var job = await _freeSql.Select<SysJobDetail>()
            .Where(a => a.GroupId == group.Id && !a.IsDeleted)
            .Where(a => a.JobName == normalizedJobName || jobKeyCandidates.Contains(a.JobKey))
            .OrderBy(a => a.Id)
            .FirstAsync(cancellationToken);

        if (job == null)
        {
            return null;
        }

        job.JobGroup = group;
        job.JobTriggers = await _freeSql.Select<SysJobTrigger>()
            .Where(a => a.JobId == job.Id && !a.IsDeleted)
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);
        job.JobHttpConfig = await _freeSql.Select<SysJobHttpConfig>()
            .Where(a => a.JobId == job.Id && !a.IsDeleted)
            .OrderByDescending(a => a.Id)
            .FirstAsync(cancellationToken);
        job.JobPluginConfig = await _freeSql.Select<SysJobPlugin>()
            .Where(a => a.JobId == job.Id)
            .OrderByDescending(a => a.Id)
            .FirstAsync(cancellationToken);

        return job;
    }
}