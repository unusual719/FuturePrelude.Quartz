namespace FuturePrelude.Quartz;

/// <summary> HTTP 配置出参 </summary>
public class HttpConfigOutput
{
    /// <summary> 主键ID </summary>
    public long Id { get; set; }

    /// <summary> 任务ID </summary>
    public long JobId { get; set; }

    /// <summary> 请求方式 </summary>
    public string RequestMethod { get; set; }

    /// <summary> Content-Type </summary>
    public string ContentType { get; set; }

    /// <summary> 请求地址 </summary>
    public string RequestUrl { get; set; }

    /// <summary> 请求头 </summary>
    public string RequestHeaders { get; set; }

    /// <summary> 请求参数 </summary>
    public string RequestBody { get; set; }

    /// <summary> 超时时间（秒） </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary> 认证类型 </summary>
    public AuthType AuthType { get; set; }

    /// <summary> 认证凭证（JSON） </summary>
    public string AuthCredentials { get; set; }

    /// <summary> 创建时间 </summary>
    public DateTime CreateTime { get; set; }
}
