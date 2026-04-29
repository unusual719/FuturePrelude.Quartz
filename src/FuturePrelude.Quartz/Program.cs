using FuturePrelude.Quartz.Listener;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// 加载所有配置文件
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.SetJsonSerializerSettings();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient(InternalConstants.HttpClientIgnoreVerifySsl)
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    });
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());
builder.Services.AddSignalR();

// Options
builder.Services.AddOptions(builder.Configuration);

// 请求体限制
builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 1024 * 1024 * 100; });
builder.Services.Configure<KestrelServerOptions>(options => { options.Limits.MaxRequestBodySize = 1024 * 1024 * 100; });

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });
builder.Services.Configure<GzipCompressionProviderOptions>(options => { options.Level = CompressionLevel.Fastest; });

var quartzOptions = builder.Configuration
    .GetSection(QuartzStoreOptions.SectionName).Get<QuartzStoreOptions>() ?? new QuartzStoreOptions();

// Redis（用于分布式锁、缓存、集群调度等，默认 Redis）
builder.Services.AddStackExchangeRedis(builder.Configuration);
builder.Services.AddSingleton<IDistributedJobLockService, RedisDistributedJobLockService>();

// FreeSql（用于业务表存储，默认 MySQL）
builder.Services.AddFreeSqlDbContext(quartzOptions);

// Quartz（调度）
builder.Services.AddQuartzScheduler(quartzOptions);

// IOC 注入
builder.Services.AddHostedService<FuturePrelude.Quartz.QuartzHostedService>();
builder.Services.AddHostedService<RedisLockCleanupHostedService>(); // 监控应用退出时释放分布式锁锁

builder.Services.AddSingleton<PolicyFactory>();
builder.Services.AddSingleton<SchedulerBootstrapService>();
builder.Services.AddSingleton<GlobalJobListener>();
builder.Services.AddSingleton<App>();
builder.Services.AddSingleton<IAuthorizationHandler, JWTAuthorizeHandler>();
builder.Services.AddSingleton<ISchedulerService, SchedulerService>();
builder.Services.AddScoped<IJobStorageService, JobStorageService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IJobManagementService, JobManagementService>();
builder.Services.AddScoped<ISysJobGroupService, SysJobGroupService>();
builder.Services.AddScoped<ISysJobDetailService, SysJobDetailService>();
builder.Services.AddScoped<ISysJobExecutionLogService, SysJobExecutionLogService>();
builder.Services.AddScoped<ISysJobTriggerService, SysJobTriggerService>();
builder.Services.AddSingleton<IDashboardNotifier, DashboardNotifier>();

#region Mapster

// Mapster 配置
var mapsterConfig = Mapster.TypeAdapterConfig.GlobalSettings;
MapsterConfig.Register(mapsterConfig);
builder.Services.AddSingleton(mapsterConfig);

#endregion Mapster

// 允许使用 X-Forwarded-For、X-Forwarded-Proto
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
});

#region MVC / 全局过滤器

builder.Services.Configure<MvcOptions>(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
});

#endregion MVC / 全局过滤器

builder.Services.AddJwt();

var app = builder.Build();

// 代理头
app.UseForwardedHeaders();
// HTTPS
app.UseHttpsRedirection();
app.UseResponseCompression();

// 中间件
app.UseMiddleware<NLogRequestIdMiddleware>();
app.UseMiddleware<RequestTimingMiddleware>();

// 启用静态文件服务
app.UseStaticFiles();

// SPA 回退：未匹配的路由返回 index.html（支持 Vue Router history 模式）
app.MapFallbackToFile("index.html");

// 路由
app.UseRouting();
app.UseCors("AllowAll");
app.UseMiddleware<ResponseWrapperMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestBodyCaptureMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<DashboardHub>("/hubs/dashboard");

// 种子数据初始化
await app.SeedDefaultJobGroupAsync();
app.SeedSystemJobsOnStarted();

// 启动日志记录
var logger = LogManager.GetCurrentClassLogger();
var urls = builder.Configuration["urls"]
        ?? builder.Configuration["Urls"]
        ?? builder.Configuration["ASPNETCORE_URLS"];
logger.Info($"环境: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}");
logger.Info($"监听地址: {urls}");
logger.Info(" FuturePrelude.QuartzScheduler 启动完成");

app.Run();
