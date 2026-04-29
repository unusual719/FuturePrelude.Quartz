namespace FuturePrelude.Quartz;

/// <summary> JobDataMapKeys </summary>
public class JobDataMapKeys
{
    /// <summary> 错误详情 </summary>
    public const string ExecutionDetails = "__execDetails";

    /// <summary> 是否成功 </summary>
    public const string IsSuccess = "__isSuccess";

    /// <summary> 返回码 </summary>
    public const string StatusCode = "__returnCode";

    /// <summary> Tigger 触发器过期结束时间 </summary>
    public const string ExpirationEndTime = "__tiggerExpirationEndTime";

    #region HTTP Job DataMapKey

    public const string HttpMethod = "__httpJobRequestMethod";
    public const string HttpUrl = "__httpJobRequestUrl";
    public const string HttpRequestBody = "__httpJobRequestBody";
    public const string HttpTimeout = "__httpJobRequestTimeout";
    public const string HttpHeaders = "__httpJobRequestHeaders";

    #endregion HTTP Job DataMapKey

    #region Plugin Job DataMapKey

    /// <summary> 插件执行描述（JSON） </summary>
    public const string PluginExecutionDescriptor = "__pluginExecutionDescriptor";

    #endregion Plugin Job DataMapKey

    /// <summary> 执行开始时间 </summary>
    public const string ExecutionStartTime = "__eecutionStartTime";

    /// <summary> 执行耗时 </summary>
    public const string ExecutionDuration = "__executionDuration";

    /// <summary> 执行异常信息 </summary>
    public const string ExecutionExceptionMessage = "__executionExceptionMessage";

    /// <summary> 执行结果内容 </summary>
    public const string ExecutionResponseContent = "__executionResponseContent";
}
