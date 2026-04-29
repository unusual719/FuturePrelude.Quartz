using FuturePrelude.Quartz.Listener;

namespace FuturePrelude.Quartz;

/// <summary> Quartz 启停托管服务 </summary>
public class QuartzHostedService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly SchedulerBootstrapService _schedulerBootstrapService;
    private readonly GlobalJobListener _globalJobListener;
    private readonly ILogger<QuartzHostedService> _logger;
    private IScheduler _scheduler;

    public QuartzHostedService(
        ISchedulerFactory schedulerFactory,
        SchedulerBootstrapService schedulerBootstrapService,
        GlobalJobListener globalJobListener,
        ILogger<QuartzHostedService> logger)
    {
        _schedulerFactory = schedulerFactory;
        _schedulerBootstrapService = schedulerBootstrapService;
        _globalJobListener = globalJobListener;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        // JobListener
        _scheduler.ListenerManager.AddJobListener(
            _globalJobListener,
            global::Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());
        await _scheduler.Start(cancellationToken);
        
        // 数据回放 await
        // _schedulerBootstrapService.SyncAsync(cancellationToken);

        _logger.LogInformation("Quartz scheduler started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_scheduler != null)
        {
            await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
            _logger.LogInformation("Quartz scheduler stopped.");
        }
    }
}