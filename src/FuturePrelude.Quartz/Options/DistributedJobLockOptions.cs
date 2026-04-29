namespace FuturePrelude.Quartz;

/// <summary> 分布式任务锁配置 </summary>
public class DistributedJobLockOptions
{
    /// <summary> 配置节点名称 </summary>
    public const string SectionName = "DistributedJobLockOptions";

    /// <summary> 是否启用分布式锁 </summary>
    public bool Enabled { get; set; } = true;

    /// <summary> Redis 锁键前缀 </summary>
    public string KeyPrefix { get; set; } = "futureprelude:quartz:lock";

    /// <summary> 默认锁超时时间（秒） </summary>
    public long DefaultTtlSeconds { get; set; } = (long)TimeSpan.FromHours(6).TotalSeconds;
}
