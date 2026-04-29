namespace FuturePrelude.Quartz;

/// <summary> HttpJob </summary>
public class HttpApiJob : IJob
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HttpApiJob> _logger;
    private readonly IJobStorageService _jobStorage;
    private readonly IWebHostEnvironment _environment;
    private readonly PolicyFactory _policyFactory;
    private readonly IDistributedJobLockService _distributedJobLockService;

    public HttpApiJob(IHttpClientFactory httpClientFactory
        , ILogger<HttpApiJob> logger
        , IJobStorageService jobStorage
        , IWebHostEnvironment environment
        , PolicyFactory policyFactory
        , IDistributedJobLockService distributedJobLockService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _jobStorage = jobStorage;
        _environment = environment;
        _policyFactory = policyFactory;
        _distributedJobLockService = distributedJobLockService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        DistributedJobLockAcquireResult? lockResult = null;

        try
        {
            // 执行时间
            context.Put(
                JobDataMapKeys.ExecutionStartTime,
                ChinaTimeZoneConverter.ToChinaOffset(ChinaTimeZoneConverter.Now()).ToString("O", CultureInfo.InvariantCulture));

            var jobName = context.JobDetail.Key.Name;
            var jobGroupName = context.JobDetail.Key.Group;
            var triggerName = context.Trigger.Key.Name;
            var triggerGroup = context.Trigger.Key.Group;

            var jobInfo = await _jobStorage.GetJobAsync(jobName, jobGroupName, context.CancellationToken);
            if (jobInfo == null)
            {
                _logger.LogError("[{runInstanceId}] 作业信息不存在（{jobGroup}.{jobName}），任务终止", context.FireInstanceId, jobGroupName, jobName);
                throw new JobExecutionException("作业信息不存在，无法运行 HttpJob");
            }

            var triggerKey = JobExecutionRuntime.BuildTriggerKey(triggerName, triggerGroup);
            var currentTrigger = JobExecutionRuntime.ResolveCurrentTrigger(jobInfo, triggerName, triggerGroup);

            var jobDataMap = context.MergedJobDataMap;
            var expiration = JobExecutionRuntime.ResolveExpiration(jobDataMap, currentTrigger);
            if (JobExecutionRuntime.IsExpired(jobDataMap, currentTrigger, DateTimeOffset.UtcNow))
            {
                // 任务暂停
                _logger.LogError(
                    "[{runInstanceId}] 任务已过期（{endTime}），任务终止",
                    context.FireInstanceId,
                    expiration.HasValue
                        ? ChinaTimeZoneConverter.FromOffset(expiration.Value).ToString("yyyy-MM-dd HH:mm:ss")
                        : null);
                context.SetIsSuccess(false);
                await context.Scheduler.PauseTrigger(new TriggerKey(context.Trigger.Key.Name, context.Trigger.Key.Group));
                return;
            }

            // 是否禁止并发执行
            if (jobInfo.DisallowConcurrent)
            {
                try
                {
                    lockResult = await _distributedJobLockService.TryAcquireAsync(
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        context.FireInstanceId,
                        context.CancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[{runInstanceId}] 获取分布式锁时发生异常，跳过执行 HttpJobJob={jobKey} Trigger={triggerKey}",
                        context.FireInstanceId,
                        context.JobDetail.Key,
                        context.Trigger.Key);
                    context.SetExecutionDetails("已跳过：分布式锁不可用");
                    return;
                }

                // 分布式锁已被占用
                if (lockResult.Status == DistributedJobLockAcquireStatus.Contended)
                {
                    _logger.LogWarning(
                        "[{runInstanceId}] 分布式锁已被占用，跳过执行 HttpJobJob={jobKey} Trigger={triggerKey} LockKey={lockKey}",
                        context.FireInstanceId,
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        lockResult.LockKey);
                    context.SetExecutionDetails("已跳过：分布式锁已被占用");
                    return;
                }

                // 分布式锁不可用
                if (lockResult.Status == DistributedJobLockAcquireStatus.Unavailable)
                {
                    _logger.LogWarning(
                        "[{runInstanceId}] 分布式锁不可用，跳过执行 HttpJobJob={jobKey} Trigger={triggerKey} LockKey={lockKey}",
                        context.FireInstanceId,
                        context.JobDetail.Key,
                        context.Trigger.Key,
                        lockResult.LockKey);
                    context.SetExecutionDetails("已跳过：分布式锁不可用");
                    return;
                }
            }

            // 请求参数
            var url = jobDataMap.GetString(JobDataMapKeys.HttpUrl) ?? jobInfo.JobHttpConfig?.RequestUrl;
            if (string.IsNullOrEmpty(url))
            {
                // 任务未配置请求 URL
                _logger.LogError("[{runInstanceId}] 无法运行 HttpJob，未指定请求 URL，任务终止", context.FireInstanceId);
                throw new JobExecutionException("无法运行 HttpJob，未指定请求 URL，任务终止");
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                _logger.LogError("[{runInstanceId}] 无法运行 HttpJob，非法 {URL}，任务终止", context.FireInstanceId, url);
                throw new JobExecutionException("无法运行 HttpJob，非法 URL，任务终止")
                {
                    UnscheduleAllTriggers = true
                };
            }

            // 请求方式
            var method = jobDataMap.GetString(JobDataMapKeys.HttpMethod) ?? jobInfo.JobHttpConfig?.RequestMethod;
            if (method == null)
            {
                _logger.LogWarning("[{runInstanceId}] 无法运行 HttpJob，未指定请求方式，任务终止",
                    context.FireInstanceId);
                throw new JobExecutionException("无法运行 HttpJob，未指定请求方式，任务终止");
            }
            var action = HttpRequestJobRuntime.ParseMethod(method);

            // 请求超时时间（默认：60s）
            int? timeout = jobDataMap.TryGetInt(JobDataMapKeys.HttpTimeout, out var x)
                ? x : jobInfo.JobHttpConfig?.TimeoutSeconds;

            // 请求头
            var headersJson = jobDataMap.GetString(JobDataMapKeys.HttpHeaders)
                ?? jobInfo.JobHttpConfig?.RequestHeaders;
            var headers = HttpRequestJobRuntime.DeserializeHeaders(headersJson);

            // 请求参数
            var requestBody = jobDataMap.GetString(JobDataMapKeys.HttpRequestBody)
                ?? jobInfo.JobHttpConfig?.RequestBody;
            var contentType = jobInfo.JobHttpConfig?.ContentType
                ?? System.Net.Mime.MediaTypeNames.Application.Json;

            // 创建 HttpClient
            HttpClient httpClient;
            if (!_environment.IsProduction())
                httpClient = _httpClientFactory.CreateClient(InternalConstants.HttpClientIgnoreVerifySsl);
            else
                httpClient = _httpClientFactory.CreateClient();

            // 配置请求超时时间，默认：Timeout.InfiniteTimeSpan
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
            if (timeout.HasValue)
            {
                if (timeout > 0)
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(timeout.Value);
                }
                else
                {
                    httpClient.Timeout = Timeout.InfiniteTimeSpan;
                }
            }

            var stopwatch = Stopwatch.StartNew();
            using var response = jobInfo.RetryOnFailure
                ? await _policyFactory.CreateRetryPolicy<HttpResponseMessage>(
                    retryCount: jobInfo.MaxRetry ?? 3,
                    baseDelaySeconds: jobInfo.RetryBackoffSeconds ?? 10,
                    maxDelaySeconds: 30,
                    timeoutSeconds: timeout ?? 60,
                    shouldRetryResult: retryResponse =>
                    {
                        var statusCode = (int)retryResponse.StatusCode;
                        return statusCode == 429 || statusCode >= 500 && statusCode <= 599;
                    })
                    .ExecuteAsync(ct => SendRequestAsync(ct), context.CancellationToken)
                : await SendRequestAsync(context.CancellationToken);
            var result = await response.Content.ReadAsStringAsync(context.CancellationToken);
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            // 执行耗时
            context.Put(JobDataMapKeys.ExecutionDuration, $"{elapsedMilliseconds}");
            context.Put(JobDataMapKeys.ExecutionResponseContent, result);

            _logger.LogInformation("[{runInstanceId}] 响应状态代码：{code}", context.FireInstanceId, response.StatusCode);

            context.Result = result;
            context.SetIsSuccess(response.IsSuccessStatusCode);
            context.SetReturnCode((int)response.StatusCode);
            context.SetExecutionDetails(
                $"""
                URL: {url}
                Method: {action}
                Status: {(int)response.StatusCode}
                Trigger: {triggerKey}
                Time: {ChinaTimeZoneConverter.Now():yyyy-MM-dd HH:mm:ss}
                RequestBody: {requestBody}
                ResponseContent: {result}
                """);

            async Task<HttpResponseMessage> SendRequestAsync(CancellationToken cancellationToken)
            {
                using var request = HttpRequestJobRuntime.BuildRequestMessage(
                    url,
                    action,
                    requestBody,
                    headers,
                    contentType,
                    jobInfo.Id,
                    $"{jobGroupName}:{jobName}",
                    currentTrigger?.Id,
                    triggerKey);

                if (jobInfo.JobHttpConfig.AuthType == AuthType.BearerToken)
                {
                    var authCredentialInfo = HttpBearerTokenRuntime.DeserializeConfig(jobInfo.JobHttpConfig.AuthCredentials);
                    if (!authCredentialInfo.NeedRequestToken)
                    {
                        // 固定 Token 直接应用
                        var token = authCredentialInfo.Token;
                        HttpBearerTokenRuntime.ApplyAuthorizationHeader(request.Headers, authCredentialInfo, token);
                    }

                    if (authCredentialInfo.NeedRequestToken)
                    {
                        // 需要请求 Token，获取 Token 后应用
                        await HttpBearerTokenRuntime
                             .ApplyBearerTokenAsync(_httpClientFactory, request, jobInfo.JobHttpConfig.AuthCredentials, cancellationToken);
                    }
                }

                _logger.LogInformation("[{runInstanceId} - HttpApiJob.Execute] 向指定的 URL '{url}' 发送 '{action}' 请求", context.FireInstanceId, url, action);
                return await httpClient.SendAsync(request, cancellationToken);
            }
        }
        catch (JobExecutionException ex)
        {
            context.SetIsSuccess(false);
            context.Put(JobDataMapKeys.ExecutionDuration, ex.ToString());
            context.SetExecutionDetails(ex.ToString());

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "执行 HttpJob 失败[{runInstanceId}]", context.FireInstanceId);
            context.SetIsSuccess(false);
            context.Put(JobDataMapKeys.ExecutionDuration, ex.ToString());
            context.SetExecutionDetails(ex.ToString());

            throw new JobExecutionException("执行 HTTP 任务失败", ex);
        }
        finally
        {
            if (lockResult?.Status == DistributedJobLockAcquireStatus.Acquired
                && !string.IsNullOrWhiteSpace(lockResult.LockToken))
            {
                await _distributedJobLockService.ReleaseAsync(
                    lockResult.LockKey,
                    lockResult.LockToken,
                    context.CancellationToken);
            }
        }
    }
}