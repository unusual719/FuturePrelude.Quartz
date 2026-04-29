using StackExchange.Redis;

namespace FuturePrelude.Quartz;

/// <summary> IServiceCollection 的扩展方法，用于注册应用程序配置选项 </summary>
public static class ServiceCollectionExtensions
{
    /// <summary> 从配置文件中绑定并注册 Quartz、JWT 和 App 的配置选项 </summary>
    /// <param name="services"> </param>
    /// <param name="configuration"> </param>
    /// <returns> </returns>
    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<QuartzStoreOptions>()
            .Bind(configuration.GetSection(QuartzStoreOptions.SectionName));
        services.AddOptions<JWTOptions>()
            .Bind(configuration.GetSection(JWTOptions.SectionName));
        services.AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName));
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName));
        services.AddOptions<DistributedJobLockOptions>()
            .Bind(configuration.GetSection(DistributedJobLockOptions.SectionName));

        return services;
    }

    /// <summary> 注册 FreeSql ORM 实例到 DI 容器 </summary>
    /// <param name="services"> </param>
    /// <param name="quartzOptions"> </param>
    /// <returns> </returns>
    public static IServiceCollection AddFreeSqlDbContext(this IServiceCollection services, QuartzStoreOptions? quartzOptions)
    {
        // 仅在配置了数据库连接字符串时注册 FreeSql
        if (!string.IsNullOrWhiteSpace(quartzOptions!.ConnectionString))
        {
            services.AddSingleton<IFreeSql>(_ =>
                new FreeSql.FreeSqlBuilder()
                    .UseLazyLoading(true)  // 启用延迟加载
                    .UseNoneCommandParameter(true) // 不使用命令参数化
                    .UseConnectionString(ConvertToFreeSqlDataType(quartzOptions.DBType), quartzOptions.ConnectionString)
                    .UseAutoSyncStructure(true) // 自动同步数据库结构
                    .Build());

            // 将 DBType 枚举转换为 FreeSql 的 DataType
            static FreeSql.DataType ConvertToFreeSqlDataType(DBType dbType)
            {
                return dbType switch
                {
                    DBType.MySql => FreeSql.DataType.MySql,
                    DBType.SqlServer => FreeSql.DataType.SqlServer,
                    DBType.PostgreSQL => FreeSql.DataType.PostgreSQL,
                    DBType.SQLite => FreeSql.DataType.Sqlite,
                    _ => throw new NotSupportedException($"不支持的数据库类型: {dbType}")
                };
            }
        }

        return services;
    }

    /// <summary> 注册 Redis 实例到 DI 容器 </summary>
    /// <param name="services"> </param>
    /// <param name="configuration"> </param>
    /// <returns> </returns>
    public static IServiceCollection AddStackExchangeRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()!;
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            ConfigurationOptions options = ConfigurationOptions.Parse(redisOptions.ConnectionString);
            options.DefaultDatabase = redisOptions.Database;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<IDatabase>(sp =>
        {
            var connection = sp.GetRequiredService<IConnectionMultiplexer>();
            return connection.GetDatabase(redisOptions.Database);
        });

        return services;
    }
}