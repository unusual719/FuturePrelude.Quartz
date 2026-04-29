namespace FuturePrelude.Quartz;

/// <summary> 应用停止时清理 Redis 分布式锁的托管服务 </summary>
public sealed class RedisLockCleanupHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IDistributedJobLockService _lockService;
    private readonly ILogger<RedisLockCleanupHostedService> _logger;

    public RedisLockCleanupHostedService(
        IHostApplicationLifetime lifetime,
        IDistributedJobLockService lockService,
        ILogger<RedisLockCleanupHostedService> logger)
    {
        _logger = logger;
        _lifetime = lifetime;
        _lockService = lockService;
    }

    private void OnStopping()
    {
        _logger.LogInformation("应用正在停止，开始释放 Redis 分布式锁...");
        _lockService.ReleaseAllAsync(default!)
            .GetAwaiter()
            .GetResult();
    }

    private void OnStopped()
    {
        _logger.LogInformation("应用已停止...");
    }

    public static void ReleaseAll(IServiceProvider serviceProvider)
    {
        try
        {
            serviceProvider.GetRequiredService<IDistributedJobLockService>()
                .ReleaseAllAsync(default!)
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetime.ApplicationStopping.Register(OnStopping);
        _lifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}