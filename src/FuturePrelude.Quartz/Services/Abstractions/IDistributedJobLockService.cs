namespace FuturePrelude.Quartz;

/// <summary> 分布式任务锁获取状态 </summary>
public enum DistributedJobLockAcquireStatus
{
    /// <summary> 成功获取锁 </summary>
    [Description("已获取锁")]
    Acquired = 1,

    /// <summary> 获取锁竞争中 </summary>
    [Description("竞争中")]
    Contended = 2,

    /// <summary> 锁不可用 </summary>
    [Description("不可用")]
    Unavailable = 3
}

/// <summary> 分布式任务锁获取结果 </summary>
/// <param name="Status"> </param>
/// <param name="LockKey"> </param>
/// <param name="LockToken"> </param>
public sealed record DistributedJobLockAcquireResult(
    DistributedJobLockAcquireStatus Status,
    string LockKey,
    string? LockToken);

/// <summary> 分布式任务锁服务定义 </summary>
public interface IDistributedJobLockService
{
    /// <summary> 尝试获取分布式锁 </summary>
    Task<DistributedJobLockAcquireResult> TryAcquireAsync(
        JobKey jobKey,
        TriggerKey triggerKey,
        string fireInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary> 释放分布式锁 </summary>
    Task ReleaseAsync(string lockKey, string lockToken, CancellationToken cancellationToken = default);

    /// <summary> 释放本实例持有的所有分布式锁（应用停止时调用） </summary>
    Task ReleaseAllAsync(CancellationToken cancellationToken = default);
}