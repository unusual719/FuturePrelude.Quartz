using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace FuturePrelude.Quartz;

/// <summary> Redis 分布式任务锁服务 </summary>
public sealed class RedisDistributedJobLockService : IDistributedJobLockService
{
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        end
        return 0
        """;

    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly DistributedJobLockOptions _options;
    private readonly ILogger<RedisDistributedJobLockService> _logger;

    /// <summary> 跟踪本实例已获取的锁（LockKey -&gt; LockToken） </summary>
    private readonly ConcurrentDictionary<string, string> _acquiredLocks = new();

    public RedisDistributedJobLockService(
        IDatabase database,
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<DistributedJobLockOptions> options,
        ILogger<RedisDistributedJobLockService> logger)
    {
        _database = database;
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
        _logger = logger;
    }

    private string BuildLockKey(JobKey jobKey, TriggerKey triggerKey)
    {
        return $"{_options.KeyPrefix}:{jobKey.Group}_{jobKey.Name}_{triggerKey.Group}_{triggerKey.Name}";
    }

    private string BuildLockToken(string fireInstanceId)
    {
        var clientName = _connectionMultiplexer.ClientName;
        return $"{_options.KeyPrefix}:{Environment.MachineName}:{clientName}_{fireInstanceId}";
    }

    private async Task ReleaseInternalAsync(string lockKey, string lockToken, CancellationToken cancellationToken)
    {
        try
        {
            await _database.ScriptEvaluateAsync(
                ReleaseScript,
                [lockKey],
                [lockToken],
                CommandFlags.None);
        }
        catch (Exception ex) when (ex is RedisException or RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "释放分布式任务锁失败，锁键：{LockKey}", lockKey);
        }
    }

    /// <inheritdoc />
    public async Task<DistributedJobLockAcquireResult> TryAcquireAsync(
        JobKey jobKey,
        TriggerKey triggerKey,
        string fireInstanceId,
        CancellationToken cancellationToken = default)
    {
        var lockKey = BuildLockKey(jobKey, triggerKey);
        if (!_options.Enabled)
        {
            return new DistributedJobLockAcquireResult(DistributedJobLockAcquireStatus.Acquired, lockKey, null);
        }

        var lockToken = BuildLockToken(fireInstanceId);

        try
        {
            var acquired = await _database.StringSetAsync(
                lockKey,
                lockToken,
                TimeSpan.FromSeconds(_options.DefaultTtlSeconds),
                When.NotExists,
                CommandFlags.None);

            if (acquired)
            {
                _acquiredLocks.TryAdd(lockKey, lockToken);
            }

            return acquired
                ? new DistributedJobLockAcquireResult(DistributedJobLockAcquireStatus.Acquired, lockKey, lockToken)
                : new DistributedJobLockAcquireResult(DistributedJobLockAcquireStatus.Contended, lockKey, null);
        }
        catch (Exception ex) when (ex is RedisException or RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "获取分布式任务锁失败，锁键：{LockKey}", lockKey);
            return new DistributedJobLockAcquireResult(DistributedJobLockAcquireStatus.Unavailable, lockKey, null);
        }
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(string lockKey, string lockToken, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(lockToken))
        {
            return;
        }

        try
        {
            await _database.ScriptEvaluateAsync(
                ReleaseScript,
                [lockKey],
                [lockToken],
                CommandFlags.None);

            _acquiredLocks.TryRemove(lockKey, out _);
        }
        catch (Exception ex) when (ex is RedisException or RedisConnectionException or RedisTimeoutException)
        {
            _logger.LogWarning(ex, "释放分布式任务锁失败，锁键：{LockKey}", lockKey);
        }
    }

    /// <inheritdoc />
    public async Task ReleaseAllAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Enabled)
        {
            var endpoints = _connectionMultiplexer.GetEndPoints();
            var server = _connectionMultiplexer.GetServer(endpoints.First());
            var keys = server.Keys(pattern: $"{_options.KeyPrefix}:*");
            _logger.LogInformation("1、正在释放本实例持有的 {Count} 个分布式锁...", keys.Count());
            foreach (var key in keys)
            {
                var clientName = _connectionMultiplexer.ClientName;
                var lockTokenPrefix = $"{_options.KeyPrefix}:{Environment.MachineName}:{clientName}";
                await _database.StringGetAsync(key).ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        var value = task.Result;
                        if (value.HasValue && value.ToString().StartsWith(lockTokenPrefix))
                        {
                            _database.KeyDeleteAsync(key);
                        }
                    }
                }, cancellationToken);
            }
        }

        _logger.LogInformation("2、正在释放本实例持有的 {Count} 个分布式锁...", _acquiredLocks.Count);
        var releaseTasks = _acquiredLocks.Select(kv =>
            ReleaseInternalAsync(kv.Key, kv.Value, cancellationToken));

        await Task.WhenAll(releaseTasks);
        _acquiredLocks.Clear();

        _logger.LogInformation("已释放所有分布式锁");
    }
}
