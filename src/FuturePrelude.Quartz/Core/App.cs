namespace FuturePrelude.Quartz.Core;

public class App
{
    /// <summary> IHostEnvironment 获取泛型主机环境 </summary>
    internal static IHostEnvironment HostEnvironment;

    /// <summary> IWebHostEnvironment 获取Web主机环境 </summary>
    internal static IWebHostEnvironment WebHostEnvironment;

    /// <summary> IServiceCollection 应用服务 </summary>
    internal static IServiceCollection InternalServices;

    /// <summary> Configuration 配置对象 </summary>
    internal static IConfiguration Configuration;

    /// <summary> ServiceProvider 根服务 </summary>
    internal static IServiceProvider ServiceProvider;

    /// <summary> 自动装载主机配置 </summary>
    /// <param name="builder"> </param>
    private static void ConfigureHostAppConfiguration(IHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((hostContext, configurationBuilder) =>
        {
            // 存储环境对象
            HostEnvironment = hostContext.HostingEnvironment;

            // 加载配置
            AddJsonFiles(configurationBuilder, hostContext.HostingEnvironment);
        });
    }

    /// <summary> 配置 Furion 框架（Web） </summary>
    /// <remarks> 此次添加 <see cref="HostBuilder" /> 参数是为了兼容 .NET 5 直接升级到 .NET 6 问题 </remarks>
    /// <param name="builder"> </param>
    /// <param name="hostBuilder"> </param>
    internal static void ConfigureApplication(IWebHostBuilder builder, IHostBuilder hostBuilder = default)
    {
        // 自动装载配置
        if (hostBuilder == default)
        {
            builder.ConfigureAppConfiguration((hostContext, configurationBuilder) =>
            {
                // 存储环境对象
                HostEnvironment = WebHostEnvironment = hostContext.HostingEnvironment;

                // 加载配置
                AddJsonFiles(configurationBuilder, hostContext.HostingEnvironment);
            });
        }
        // 自动装载配置
        else ConfigureHostAppConfiguration(hostBuilder);

        // 应用初始化服务
        builder.ConfigureServices((hostContext, services) =>
        {
            // 存储配置对象
            Configuration = hostContext.Configuration;

            // 存储服务提供器
            InternalServices = services;

            // 存储根服务（解决 Web 主机还未启动时在 HostedService 中使用 App.GetService 问题
            services.AddHostedService<GenericHostLifetimeEventsHostedService>();

            // 注册 Startup 过滤器
            services.AddTransient<IStartupFilter, StartupFilter>();

            // 注册 HttpContextAccessor 服务
            services.AddHttpContextAccessor();

            // 注册内存和分布式内存
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            // 默认内置 GBK，Windows-1252, Shift-JIS, GB2312 编码支持
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        });
    }

    /// <summary> 加载自定义 .json 配置文件 </summary>
    /// <param name="configurationBuilder"> </param>
    /// <param name="hostEnvironment"> </param>
    internal static void AddJsonFiles(IConfigurationBuilder configurationBuilder, IHostEnvironment hostEnvironment)
    {
        // 获取程序执行目录
        var executeDirectory = AppContext.BaseDirectory;
        configurationBuilder.SetBasePath(executeDirectory);

        var envName = hostEnvironment?.EnvironmentName
            ?? Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT")
            ?? "Unknown";

        // 扫描执行目录及自定义配置目录下的 *.json 文件
        var jsonFiles = new[] { executeDirectory }
            .SelectMany(u => Directory.GetFiles(u, "*.json", SearchOption.TopDirectoryOnly))
            .Where(f => !f.EndsWith($"appsettings.{envName}.json", StringComparison.OrdinalIgnoreCase));

        // 如果没有配置文件，中止执行
        if (!jsonFiles.Any()) return;

        foreach (var file in jsonFiles)
        {
            configurationBuilder
                .AddJsonFile(Path.GetFileName(file), optional: true, reloadOnChange: true);
        }
    }

    /// <summary> 读取 Options 选项配置 </summary>
    /// <typeparam name="T"> </typeparam>
    /// <returns> </returns>
    /// <exception cref="InvalidOperationException"> </exception>
    public static T GetOptions<T>() where T : new()
    {
        if (Configuration == null)
        {
            throw new InvalidOperationException("Application configuration has not been initialized.");
        }

        var sectionName = typeof(T)
            .GetField("SectionName", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null)?.ToString();

        var options = Configuration
            .GetSection(sectionName)
            .Get<T>();

        return options;
    }
}