namespace FuturePrelude.Quartz;

/// <summary> HTTP 配置入参 </summary>
public class HttpConfigInput
{
    /// <summary> 请求方式 </summary>
    public string RequestMethod { get; set; }

    /// <summary> Content-Type </summary>
    public string ContentType { get; set; } = System.Net.Mime.MediaTypeNames.Application.Json;

    /// <summary> 请求地址 </summary>
    public string RequestUrl { get; set; }

    /// <summary> 请求头（JSON 格式） </summary>
    public string RequestHeaders { get; set; }

    /// <summary> 请求参数 </summary>
    public string RequestBody { get; set; }

    /// <summary> 超时时间（秒） </summary>
    public int TimeoutSeconds { get; set; } = (int)TimeSpan.FromHours(1).TotalSeconds;

    /// <summary> 认证类型 </summary>
    public AuthType AuthType { get; set; } = AuthType.None;

    /// <summary> 认证凭证（JSON） </summary>
    public string? AuthCredentials { get; set; } = "{}";
}
