namespace FuturePrelude.Quartz;

/// <summary> Quartz 注册扩展 </summary>
public static class QuartzRegistrationServiceCollectionExtensions
{
    /// <summary> 注册 Quartz 调度器及后台托管服务 </summary>
    /// <remarks> 默认使用内存存储；仅在显式启用持久化且提供连接字符串时才切换到数据库存储 </remarks>
    /// <param name="services"> </param>
    /// <param name="quartzOptions"> </param>
    /// <returns> </returns>
    public static IServiceCollection AddQuartzScheduler(this IServiceCollection services, QuartzStoreOptions? quartzOptions)
    {
        // 兜底默认值，避免未配置时在启动阶段出现空引用
        var options = quartzOptions ?? new QuartzStoreOptions();
        var threadCount = options.ThreadCount > 0 ? options.ThreadCount : 10;

        services.AddQuartz(configurator =>
        {
            configurator.SchedulerId = "AUTO";
            configurator.SchedulerName = "FuturePreludeScheduler";

            // 使用 Quartz 默认线程池，并允许通过配置覆盖最大并发数
            configurator.UseDefaultThreadPool(threadPool =>
            {
                // 同时最多执行多少个 Job
                threadPool.MaxConcurrency = Math.Clamp(threadCount, 5, 100);
            });

            // 持久化必须显式开启：仅有业务库连接字符串时，仍然回退到内存存储， 避免在未创建 QRTZ_* 表的环境中触发 schema validation 启动失败
            if (!options.UsePersistentStore || string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                configurator.UseInMemoryStore();
                return;
            }

            // 开启 Quartz AdoJobStore，作业/触发器数据落到数据库中
            configurator.UsePersistentStore(store =>
            {
                store.UseProperties = true;
                store.UseGenericDatabase(options.DbProviderName, database =>
                {
                    database.ConnectionString = options.ConnectionString;
                    database.TablePrefix = string.IsNullOrWhiteSpace(options.TablePrefix) ? "QRTZ_" : options.TablePrefix;
                });
                store.UseNewtonsoftJsonSerializer();
            });
        });

        // 由 ASP.NET Core 托管 Quartz 生命周期，应用退出时等待正在执行的任务完成
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
