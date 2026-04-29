namespace FuturePrelude.Quartz;

/// <summary> Job 类型 </summary>
public enum JobType
{
    /// <summary> Plugin Job </summary>
    [Description("Plugin")]
    AssemblyPluginJob,

    /// <summary> HttpApi Job </summary>
    [Description("HttpApi")]
    HttpApiJob
}
